using PhotoVideoBackupAPI.Services;
using PhotoVideoBackupAPI.Data;
using PhotoVideoBackupAPI.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text;

// Stage 1: Bootstrap logger — captures fatal errors during host setup before
// IConfiguration is available. Replaced by the full logger once the host is built.
SerilogConfiguration.CreateBootstrapLogger();

try
{
    Log.Information("Starting PhotoVideoBackupAPI");

    var builder = WebApplication.CreateBuilder(args);

    // Must run first — clears default config sources and adds our custom chain
    ConfigureConfiguration(builder);

    // Stage 2: Replace bootstrap logger with full Serilog logger now that
    // IConfiguration is built and the Serilog section in appsettings.json is readable.
    builder.Host.AddSerilogLogging();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Mobile Media Backup API", Version = "v1" });
        c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
        c.DocInclusionPredicate((name, api) => true);
    });

    // Add Entity Framework
    builder.Services.AddDbContext<MediaBackupDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Register services
    builder.Services.AddScoped<IMediaBackupService, MediaBackupService>();
    builder.Services.AddScoped<IAuthService, JwtAuthService>();

    // Configure JWT Authentication
    // Environment variables override appsettings.json automatically
    // Use Jwt__Secret or Jwt:Secret environment variable
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? (builder.Environment.IsDevelopment()
            ? "development-key-change-in-production"
            : throw new InvalidOperationException("JWT Secret must be set via Jwt__Secret environment variable in production"));
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PhotoVideoBackupAPI";

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

    // Add CORS
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

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mobile Media Backup API v1");
            c.RoutePrefix = string.Empty; // Serve Swagger UI at root
        });
    }

    app.UseHttpsRedirection();

    // Request logging middleware — reads Serilog:RequestLogging config to determine
    // whether to log all requests, errors only, or skip entirely.
    app.UseConfiguredRequestLogging(builder.Configuration);

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

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
        .AddEnvironmentVariables(prefix: BuildEnvironmentVariablePrefix(environmentName));
}

static string BuildEnvironmentVariablePrefix(string environmentName)
{
    if (string.IsNullOrWhiteSpace(environmentName))
    {
        return "PixNest_";
    }

    var normalized = environmentName.Trim();

    switch (normalized.ToLowerInvariant())
    {
        case "production":
        case "prod":
            return "PixNest_Prod_";
        case "staging":
            return "PixNest_Staging_";
        case "development":
        case "dev":
            return "PixNest_Dev_";
        default:
            return $"PixNest_{normalized}_";
    }
}
