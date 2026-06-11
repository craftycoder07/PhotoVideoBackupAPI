using PixNestAPI.Application;
using PixNestAPI.Infrastructure;
using PixNestAPI.Infrastructure.Logging;
using PixNestAPI.Infrastructure.Persistence;
using PixNestAPI.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    builder.Host.AddSerilogLogging();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "PixNest API", Version = "v1" });
        c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
        c.DocInclusionPredicate((name, api) => true);
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Configure JWT Authentication
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? (builder.Environment.IsDevelopment()
            ? "development-key-change-in-production"
            : throw new InvalidOperationException("JWT Secret must be set via Jwt__Secret environment variable in production"));
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

    // Skip HTTPS redirect inside Docker — TLS terminates at the reverse proxy.
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseExceptionHandler();

    // Request logging middleware — reads Serilog:RequestLogging config to determine
    // whether to log all requests, errors only, or skip entirely.
    app.UseConfiguredRequestLogging(builder.Configuration);

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

    // Auto-apply pending EF Core migrations on startup in Development.
    // In Production: generate a SQL script with `dotnet ef migrations script --idempotent`
    // and apply it directly on the server.
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is thrown intentionally by EF Core during
    // 'dotnet ef migrations add' — catching it would break migration commands.
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
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
