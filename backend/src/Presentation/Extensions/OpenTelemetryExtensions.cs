#if(DEBUG)
using ExportProcessorType = OpenTelemetry.ExportProcessorType;
#endif
using System.Data;
using System.Diagnostics;
using Core.Configs;
using Core.Constants;
using Infrastructure.Constants;
using Infrastructure.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using HeaderNames = Presentation.Constants.HeaderNames;

namespace Presentation.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection RegisterOpenTelemetry(this IServiceCollection services, AppConfig appConfig)
    {
        services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(OpenTelemetryConstants.ServiceName, serviceVersion: appConfig.Version);
                resource.AddAttributes([
                    new KeyValuePair<string, object>(OpenTelemetryTags.ServiceName, OpenTelemetryConstants.ServiceName),
                    new KeyValuePair<string, object>(OpenTelemetryTags.Environment, appConfig.Environment.ToString()),
                    new KeyValuePair<string, object>(OpenTelemetryTags.HostName, Environment.MachineName)
                ]);

                resource.AddEnvironmentVariableDetector();
                resource.AddTelemetrySdk();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(OpenTelemetryConstants.MeterName);
                metrics.AddProcessInstrumentation();
                metrics.AddRuntimeInstrumentation();
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddOtlpExporter(options => { options.Endpoint = new Uri(appConfig.ObservabilityConfig.OtelCollectorEndpoint); });

                metrics.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(OpenTelemetryConstants.ServiceName, serviceVersion: appConfig.Version));
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(OpenTelemetryConstants.ServiceName);
                tracing.AddHttpClientInstrumentation();
                tracing.AddProcessor<ActivityNameProcessor>();

                tracing.AddQuartzInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Enrich = (activity, eventName, _) =>
                    {
                        activity.SetTag(OpenTelemetryTags.QuartzJobName, eventName);
                    };
                });

                tracing.AddAspNetCoreInstrumentation(options =>
                {
                    options.EnrichWithHttpRequest = (activity, httpRequest) =>
                    {
                        activity.SetTag(OpenTelemetryTags.HttpMethod, httpRequest.Method);
                        activity.SetTag(OpenTelemetryTags.HttpPathGroup, httpRequest.Path);
                        activity.SetTag(OpenTelemetryTags.HttpRequestProtocol, httpRequest.Protocol);
                        activity.SetTag(OpenTelemetryTags.HttpRequestContentLength, httpRequest.ContentLength);
                        activity.SetTag(OpenTelemetryTags.HttpContentType, httpRequest.ContentType);
                        activity.SetTag(OpenTelemetryTags.HttpRoute, httpRequest.Path);
                        ProcessRequestHeaders(httpRequest, activity);
                    };

                    options.EnrichWithHttpResponse = (activity, httpResponse) =>
                    {
                        activity.SetTag(OpenTelemetryTags.HttpResponseContentLength, httpResponse.ContentLength);
                        activity.SetTag(OpenTelemetryTags.HttpContentType, httpResponse.ContentType);
                        activity.SetTag(OpenTelemetryTags.HttpStatusCode, httpResponse.StatusCode);
                        ProcessResponseHeaders(httpResponse, activity);
                    };

                    options.RecordException = true;
                    options.EnrichWithException = (activity, exception) =>
                    {
                        activity.SetTag(OpenTelemetryTags.ExceptionType, exception.GetType().ToString());
                    };
                });

                tracing.AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.EnrichWithIDbCommand = (activity, command) =>
                    {
                        activity.DisplayName = $"EntityFramework.{GetStatementType(command)}";
                        activity.SetTag(OpenTelemetryTags.DbCommandType, command.CommandType);
                        activity.SetTag(OpenTelemetryTags.DbCommandText, command.CommandText);
                        activity.SetTag(OpenTelemetryTags.DbIsTransaction, command.Transaction != null);
                    };

                    options.Filter = (_, command) => DatabaseCommandShouldBeFiltered(command);
                });

                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(appConfig.ObservabilityConfig.OtelCollectorEndpoint);
#if(DEBUG)
                    options.ExportProcessorType = ExportProcessorType.Simple;
#endif
                });
            });

        return services;
    }

    private static void ProcessRequestHeaders(HttpRequest httpRequest, Activity activity)
    {
        activity.SetTag(OpenTelemetryTags.UrlDetailsPath, httpRequest.Path);
        activity.SetTag(OpenTelemetryTags.UrlDetailsScheme, httpRequest.Scheme);

        foreach (var (key, value) in httpRequest.Headers)
        {
            switch (key)
            {
                case HeaderNames.Host:
                    activity.SetTag(OpenTelemetryTags.HttpHost, value);
                    activity.SetTag(OpenTelemetryTags.HttpUrl, $"{httpRequest.Scheme}://{value}{httpRequest.Path}");
                    activity.SetTag(OpenTelemetryTags.UrlDetailsHost, value);
                    break;

                case HeaderNames.UserAgent:
                    activity.SetTag(OpenTelemetryTags.HttpUserAgent, value);
                    break;
            }
        }
    }

    private static void ProcessResponseHeaders(HttpResponse httpResponse, Activity activity)
    {
        foreach (var (key, value) in httpResponse.Headers)
        {
            switch (key)
            {
                case HeaderNames.CorrelationId:
                    activity.SetTag(OpenTelemetryTags.CorrelationId, value);
                    break;
            }
        }
    }

    private static bool DatabaseCommandShouldBeFiltered(IDbCommand command)
    {
        return command.CommandText switch
        {
            var x when x.Contains("UPDATE \"InboxState\"", StringComparison.InvariantCultureIgnoreCase) => false,
            var x when x.Contains("DELETE FROM \"InboxState\"", StringComparison.InvariantCultureIgnoreCase) => false,
            var x when x.Contains("SELECT * FROM \"InboxState\"", StringComparison.InvariantCultureIgnoreCase) => false,
            var x when x.Contains("FROM \"OutboxMessage\"", StringComparison.InvariantCultureIgnoreCase) => false,
            var x when x.Contains("INSERT INTO \"InboxState\"", StringComparison.InvariantCultureIgnoreCase) => false,
            var x when x.Contains("FROM \"OutboxState\"", StringComparison.InvariantCultureIgnoreCase) => false,
            var x when x.Contains("FROM \"InboxState\"", StringComparison.InvariantCultureIgnoreCase) => false,
            var x when x.Contains("UPDATE \"OutboxState\"", StringComparison.InvariantCultureIgnoreCase) => false,
            var x when x.Contains("DELETE FROM \"OutboxState\"", StringComparison.InvariantCultureIgnoreCase) => false,
            _ => true
        };
    }

    private static string GetStatementType(IDbCommand command)
    {
        const string updateCommand = "UPDATE";
        const string insertCommand = "INSERT";
        const string deleteCommand = "DELETE";
        const string selectCommand = "SELECT";
        const string multiCommand = "MULTI";
        const string unknownCommand = "UNKNOWN";

        return command.CommandText switch
        {
            var x when x.Contains(updateCommand) && x.Contains(insertCommand) => multiCommand,
            var x when x.Contains(updateCommand) && x.Contains(deleteCommand) => multiCommand,
            var x when x.Contains(insertCommand) && x.Contains(deleteCommand) => multiCommand,
            var x when x.Contains(selectCommand) => selectCommand,
            var x when x.Contains(insertCommand) => insertCommand,
            var x when x.Contains(updateCommand) => updateCommand,
            var x when x.Contains(deleteCommand) => deleteCommand,
            _ => unknownCommand
        };
    }
}