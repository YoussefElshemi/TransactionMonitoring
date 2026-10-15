using Core.Interfaces.Services;
using Core.Models.ApiVersions;
using Core.ValueObjects;
using Presentation.Constants;

namespace Presentation.Extensions;

public enum ApiVersionSource
{
    Header,
    Pin,
    Default
}

public readonly record struct ResolvedApiVersion(ApiVersionDefinition Definition, ApiVersionSource Source);

public static class ApiVersionResolver
{
    public static ResolvedApiVersion Resolve(HttpContext httpContext, IApiVersionService apiVersionService)
    {
        if (ApiVersion.TryParse(httpContext.Request.Headers[HeaderNames.ApiVersion], out var requested) &&
            apiVersionService.Find(requested) is { } named)
        {
            return new ResolvedApiVersion(named, ApiVersionSource.Header);
        }

        if (!httpContext.User.IsApiClient())
        {
            return new ResolvedApiVersion(apiVersionService.Current, ApiVersionSource.Default);
        }

        var pinned = ApiVersion.TryParse(httpContext.User.FindApiVersion(), out var claimed)
            ? claimed
            : apiVersionService.Earliest.Version;

        return new ResolvedApiVersion(apiVersionService.Resolve(pinned), ApiVersionSource.Pin);
    }
}
