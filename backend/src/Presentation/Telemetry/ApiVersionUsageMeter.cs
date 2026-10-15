using System.Diagnostics;
using System.Diagnostics.Metrics;
using Core.Constants;
using Infrastructure.Constants;
using Presentation.Extensions;

namespace Presentation.Telemetry;

public sealed class ApiVersionUsageMeter : IDisposable
{
    public const string InstrumentName = "api.version.requests";

    private readonly Meter _meter = new(OpenTelemetryConstants.MeterName);
    private readonly Counter<long> _requests;

    public ApiVersionUsageMeter()
    {
        _requests = _meter.CreateCounter<long>(
            InstrumentName,
            description: "Authenticated requests served, by the API version they were served under.");
    }

    public void Record(HttpContext httpContext, ResolvedApiVersion resolved)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var isApiClient = httpContext.User.IsApiClient();

        var tags = new TagList
        {
            { OpenTelemetryTags.ApiVersion, resolved.Definition.Version.ToString() },
            { OpenTelemetryTags.ApiVersionSource, resolved.Source.ToString().ToLowerInvariant() },
            { OpenTelemetryTags.ApiVersionDeprecated, resolved.Definition.DeprecatedOn.HasValue },
            { OpenTelemetryTags.ApiCaller, isApiClient ? "api_client" : "user" }
        };

        if (isApiClient && TryGetOrganizationId(httpContext, out var organizationId))
        {
            tags.Add(OpenTelemetryTags.OrganizationId, organizationId.ToString());
        }

        _requests.Add(1, tags);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    private static bool TryGetOrganizationId(HttpContext httpContext, out Guid organizationId)
    {
        try
        {
            organizationId = httpContext.User.GetOrganizationId();

            return true;
        }
        catch (InvalidOperationException)
        {
            organizationId = Guid.Empty;

            return false;
        }
    }
}
