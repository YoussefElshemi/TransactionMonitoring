using System.Globalization;
using Core.Constants;
using Core.Interfaces.Services;
using Core.Models.ApiVersions;
using Core.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Presentation.Extensions;
using Presentation.Telemetry;
using HeaderNames = Presentation.Constants.HeaderNames;

namespace Presentation.Middlewares;

public class ApiVersionMiddleware(
    RequestDelegate next,
    IApiVersionService apiVersionService,
    ApiVersionUsageMeter apiVersionUsageMeter)
{
    public const string CataloguePath = "/api-versions";

    public async Task InvokeAsync(HttpContext context)
    {
        var requested = context.Request.Headers[HeaderNames.ApiVersion].ToString();

        if (!string.IsNullOrWhiteSpace(requested) && !IsServed(requested))
        {
            await RejectAsync(context, requested);

            return;
        }

        context.Response.OnStarting(() =>
        {
            WriteHeaders(context.Response, ApiVersionResolver.Resolve(context, apiVersionService).Definition);

            return Task.CompletedTask;
        });

        try
        {
            await next(context);
        }
        finally
        {
            apiVersionUsageMeter.Record(context, ApiVersionResolver.Resolve(context, apiVersionService));
        }
    }

    private bool IsServed(string requested)
    {
        return ApiVersion.TryParse(requested, out var apiVersion) && apiVersionService.Find(apiVersion) != null;
    }

    private static void WriteHeaders(HttpResponse response, ApiVersionDefinition definition)
    {
        response.Headers[HeaderNames.ApiVersion] = definition.Version.ToString();

        if (!definition.DeprecatedOn.HasValue)
        {
            return;
        }

        // RFC 9745: a structured-field date, which is an @ and a count of seconds.
        response.Headers[HeaderNames.Deprecation] =
            "@" + AtMidnightUtc(definition.DeprecatedOn.Value).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        // RFC 8594: an HTTP-date.
        if (definition.SunsetOn.HasValue)
        {
            response.Headers[HeaderNames.Sunset] =
                AtMidnightUtc(definition.SunsetOn.Value).ToString("R", CultureInfo.InvariantCulture);
        }

        response.Headers.Append(HeaderNames.Link, $"<{CataloguePath}>; rel=\"deprecation\"; type=\"application/json\"");
    }

    private Task RejectAsync(HttpContext context, string requested)
    {
        var catalogue = apiVersionService.GetCatalogue();

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Unsupported API version",
            Detail =
                $"'{requested}' is not an API version this installation serves. " +
                $"Omit {HeaderNames.ApiVersion} to be served the version your client is pinned to.",
            Instance = context.Request.Path
        }.WithContract(context, ErrorCodes.UnsupportedApiVersion);

        problemDetails.Extensions["servedVersions"] = catalogue.Versions.Select(x => x.Version.ToString()).ToArray();

        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        return context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }

    private static DateTimeOffset AtMidnightUtc(DateOnly date)
    {
        return new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
    }
}
