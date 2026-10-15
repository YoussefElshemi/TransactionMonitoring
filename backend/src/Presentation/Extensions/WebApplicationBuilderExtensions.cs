using Core.Configs;
using Core.Constants;
using Core.Exceptions;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.Filters;
using Serilog.Formatting.Compact;
using Serilog.Sinks.OpenTelemetry;
using Environment = Core.Enums.Environment;
#if (!DEBUG)
using Serilog.Filters;
#endif

namespace Presentation.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder, AppConfig appConfig)
    {
        builder.Host.UseSerilog((context, loggerConfiguration) =>
        {
            var aspnetMinLogLevel =
                Enum.TryParse<LogEventLevel>(context.Configuration["Logging:LogLevel:Microsoft:AspNetCore"], true, out var minLevel)
                    ? minLevel
                    : LogEventLevel.Warning;

            loggerConfiguration
#if(DEBUG)
                .MinimumLevel.Override("MassTransit", LogEventLevel.Debug)
#endif
                .MinimumLevel.Override("Microsoft.AspNetCore", aspnetMinLogLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty(LogPropertyNames.ServiceName, OpenTelemetryConstants.ServiceName)
                .Enrich.WithProperty(LogPropertyNames.Version, appConfig.Version)
                .Enrich.WithProperty(LogPropertyNames.Environment, appConfig.Environment.ToString())
                .Filter.ByExcluding("RequestPath like '/health%'")
                .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                    .WithDefaultDestructurers()

                    // Schema violations carry payment values; without this filter destructuring would log rejected payloads in full.
                    .WithFilter(new IgnorePropertyByNameExceptionFilter(
                        nameof(SchemaViolationException.Violations))));
#if(!DEBUG)
            loggerConfiguration
                .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics[0]"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics[1]"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics[2]"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Routing.EndpointMiddleware"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Mvc.Infrastructure.ObjectResultExecutor"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Mvc.StaticFiles.StaticFileMiddleware"))
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore"));
#endif
            if (appConfig.ObservabilityConfig.OutputLogsToConsole)
            {
                if (appConfig.Environment == Environment.Local)
                {
                    loggerConfiguration.WriteTo.Console();
                }
                else
                {
                    loggerConfiguration.WriteTo.Console(new RenderedCompactJsonFormatter());
                }
            }

            if (appConfig.ObservabilityConfig.OutputLogsToOtel)
            {
                loggerConfiguration.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = appConfig.ObservabilityConfig.OtelCollectorEndpoint;
                    options.HttpMessageHandler = new SocketsHttpHandler { ActivityHeadersPropagator = null };
                    options.IncludedData =
                        IncludedData.SpanIdField
                        | IncludedData.TraceIdField
                        | IncludedData.MessageTemplateTextAttribute;

                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        [LogPropertyNames.ServiceName] = OpenTelemetryConstants.ServiceName,
                        [LogPropertyNames.Version] = appConfig.Version,
                        [LogPropertyNames.Environment] = appConfig.Environment.ToString()
                    };
                });
            }
        });

        builder.Services.AddHttpContextAccessor();

        return builder;
    }
}