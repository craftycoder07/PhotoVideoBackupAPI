using PixNestAPI.Application;
using PixNestAPI.Infrastructure;
using PixNestAPI.Infrastructure.Logging;
using PixNestAPI.Infrastructure.Persistence;
using PixNestAPI.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

// Stage 1: Bootstrap logger — captures fatal errors during host setup before
// IConfiguration is available. Replaced by the full logger once the host is built.
SerilogConfiguration.CreateBootstrapLogger();

try
{
    Log.Information("Starting PixNestAPI");

    var builder = WebApplication.CreateBuilder(args);

    // Must run first — clears default config sources and adds our custom chain
    ConfigureConfiguration(builder);

    // Stage 2: Replace bootstrap logger with full Serilog logger now that
    // IConfiguration is built and the Serilog section in appsettings.json is readable.
    builder.Services.AddSerilogLogging();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "PixNest API", Version = "v1" });
        c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
        c.DocInclusionPredicate((name, api) => true);
    });

    // Fail fast with an actionable message instead of letting Npgsql throw a connection
    // string parse error on the first request. Skipped at design time so `dotnet ef`
    // commands still work without the environment variables being set.
    if (!EF.IsDesignTime && string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is not set. Supply it via the "
            + "ConnectionStrings__DefaultConnection environment variable.");
    }

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Behind a TLS-terminating reverse proxy (Caddy), honour X-Forwarded-Proto and
    // X-Forwarded-For so Request.Scheme, Request.IsHttps and RemoteIpAddress describe
    // the original client request rather than the proxy hop.
    //
    // KnownNetworks/KnownProxies default to loopback only. Caddy runs in a separate
    // container with a dynamic bridge IP, so with the defaults the headers would be
    // silently discarded and the scheme would stay http. Clearing both trust lists means
    // anything able to reach this port can spoof X-Forwarded-*, which is safe ONLY
    // because docker-compose.yml does not publish the API port — Caddy on the shared
    // pixnest-net is the sole route in. Do not publish the port without revisiting this.
    //
    // X-Forwarded-Host is deliberately NOT forwarded: nothing here needs it, and it would
    // let the Host header be set from a request header.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;   // exactly one proxy hop: Caddy
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // Configure JWT Authentication.
    // Outside Development the signing key must come from configuration — there is no
    // fallback, and a too-short key is rejected here rather than failing later inside
    // SymmetricSecurityKey with an opaque message.
    var jwtSecret = builder.Configuration["Jwt:Secret"];
    if (builder.Environment.IsDevelopment() || EF.IsDesignTime)
    {
        jwtSecret = string.IsNullOrWhiteSpace(jwtSecret)
            ? "development-key-change-in-production"
            : jwtSecret;
    }
    else if (string.IsNullOrWhiteSpace(jwtSecret))
    {
        throw new InvalidOperationException(
            "Jwt:Secret is not set. Supply it via the Jwt__Secret environment variable "
            + "outside Development.");
    }
    else if (jwtSecret.Length < 32)
    {
        throw new InvalidOperationException(
            $"Jwt:Secret must be at least 32 characters for HS256 signing (got {jwtSecret.Length}).");
    }

    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PixNestAPI";

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // Must be the FIRST middleware — everything downstream (exception handler, request
    // logging, controllers) should see the client's scheme and IP, not the proxy's.
    /*
     * When Reverse proxy forwards request to API following things happen
     * 1) API sees Reverse Proxy's address as senders address
     * 2) Reverse proxy changes request from HTTPS to HTTP so scheme is also invalid
     * You want your API to know actual IP address of your client for logging, rate limiting etc.
     * Following middleware intercepts the request and overrides properties on HttpContext so other components function correctly based on what is configured in ForwardedHeadersOptions. (HttpContext.Connection.RemoteIpAddress & HttpContext.Request.Scheme)
     */
    app.UseForwardedHeaders();

    // Show Swagger in Development (Docker runs HTTP-only; no TLS cert inside container).
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "PixNest API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    // No UseHttpsRedirection: Kestrel only ever listens on HTTP and holds no certificate.
    // TLS termination and the http -> https redirect are Caddy's job at the edge.
    // Redirecting here would bounce the request back to the proxy, which forwards it over
    // HTTP again — an infinite loop.

    app.UseExceptionHandler();

    // Request logging middleware — reads Serilog:RequestLogging config to determine
    // whether to log all requests, errors only, or skip entirely.
    app.UseConfiguredRequestLogging(builder.Configuration);

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // GET and HEAD: uptime monitors and load balancers commonly probe with HEAD, and
    // MapGet alone answers those with 405.
    app.MapMethods("/health", new[] { "GET", "HEAD" },
        () => new { status = "healthy", timestamp = DateTime.UtcNow });

    // Auto-apply pending EF Core migrations on startup, in every environment. This is a
    // single-instance deployment — no replicas and no rolling deploys, so there is no
    // second container that could race the same migration.
    // Set Database:MigrateOnStartup=false to opt out and apply a script manually instead
    // (`dotnet ef migrations script --idempotent`).
    if (app.Configuration.GetValue("Database:MigrateOnStartup", defaultValue: true))
    {
        await ApplyMigrationsAsync(app);
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is thrown intentionally by EF Core during
    // 'dotnet ef migrations add' — catching it would break migration commands.
    Log.Fatal(ex, "Application terminated unexpectedly");

    // Report failure to Docker: without this the process exits 0 and a fatal
    // configuration error is indistinguishable from a clean shutdown.
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

// Applies pending migrations with a bounded retry.
//
// depends_on: service_healthy already waits for Postgres, but on a home server that
// reboots (power cut, unattended updates) the health check can pass a moment before
// Postgres actually accepts connections. Retrying beats crash-looping the container.
// After the final attempt the exception propagates: the process logs it as Fatal, exits
// non-zero, and Docker's restart policy takes over.
static async Task ApplyMigrationsAsync(WebApplication app)
{
    const int maxAttempts = 5;
    var delay = TimeSpan.FromSeconds(3);

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count == 0)
            {
                Log.Information("Database schema is up to date — no pending migrations");
                return;
            }

            Log.Information("Applying {Count} pending migration(s): {Migrations}",
                pending.Count, string.Join(", ", pending));

            await db.Database.MigrateAsync();

            Log.Information("Migrations applied successfully");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            Log.Warning(ex, "Migration attempt {Attempt}/{MaxAttempts} failed; retrying in {Delay}s",
                attempt, maxAttempts, delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }
}

static void ConfigureConfiguration(WebApplicationBuilder builder)
{
    var environmentName = builder.Environment.EnvironmentName;

    builder.Configuration.Sources.Clear();

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddEnvironmentVariables(prefix: BuildEnvironmentVariablePrefix(environmentName));
}

static string BuildEnvironmentVariablePrefix(string environmentName)
{
    if (string.IsNullOrWhiteSpace(environmentName))
        return "PixNest_";

    return environmentName.Trim().ToLowerInvariant() switch
    {
        "production" or "prod" => "PixNest_Prod_",
        "staging" => "PixNest_Staging_",
        "development" or "dev" => "PixNest_Dev_",
        var other => $"PixNest_{other}_"
    };
}
