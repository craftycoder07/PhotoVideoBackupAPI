using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace PixNestAPI.Infrastructure.Logging;

public static class SerilogConfiguration
{
    /// <summary>
    /// Lightweight bootstrap logger for fatal startup errors.
    /// Runs before IConfiguration is built — console only.
    /// </summary>
    public static void CreateBootstrapLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] BOOTSTRAP {Message:lj}{NewLine}{Exception}")
            .CreateBootstrapLogger();
    }

    /// <summary>
    /// Full Serilog configuration via IConfiguration (reads appsettings Serilog section).
    /// Call AFTER ConfigureConfiguration() so the Serilog section is visible.
    /// </summary>
    public static IServiceCollection AddSerilogLogging(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddSerilog((servicesProvider, loggerConfiguration) =>
        {
            IWebHostEnvironment env = servicesProvider.GetRequiredService<IWebHostEnvironment>();
            IConfiguration configuration = servicesProvider.GetRequiredService<IConfiguration>();
            
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(servicesProvider)   // picks up any ILogEventEnricher registered in DI
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessId()
                .Enrich.WithProperty("Application", "PixNestAPI");

            // Fallback if no Serilog section exists in config
            if (!configuration?.GetSection("Serilog").Exists() ?? true)
            {
                if (env?.IsDevelopment() ?? false)
                    loggerConfiguration.WriteTo.Console();
                else
                    loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
            }
        });
    }

    /// <summary>
    /// Conditionally adds Serilog request logging middleware based on appsettings.
    ///
    /// Config keys (under Serilog:RequestLogging):
    ///   Enabled    — bool, defaults to true. Set false to skip middleware entirely.
    ///   ErrorsOnly — bool, defaults to false. Set true to log only 4xx/5xx and exceptions;
    ///                successful requests are silently dropped.
    ///
    /// Place BEFORE UseCors/UseAuthentication in the app pipeline.
    /// </summary>
    public static IApplicationBuilder UseConfiguredRequestLogging(
        this IApplicationBuilder app, IConfiguration configuration)
    {
        var section = configuration.GetSection("Serilog:RequestLogging");
        var enabled = section.GetValue("Enabled", defaultValue: true);

        if (!enabled)
            return app;

        var errorsOnly = section.GetValue("ErrorsOnly", defaultValue: false);

        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null) return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;

                // When ErrorsOnly = true, return Verbose for successful requests.
                // Verbose is below all configured minimum levels, so these events
                // are discarded by every sink without appearing in any output.
                if (errorsOnly) return LogEventLevel.Verbose;

                if (httpContext.Request.Path.StartsWithSegments("/health")) return LogEventLevel.Debug;
                if (httpContext.Request.Path.StartsWithSegments("/swagger")) return LogEventLevel.Debug;
                return LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent",
                    httpContext.Request.Headers["User-Agent"].FirstOrDefault());
            };
        });
    }
}
