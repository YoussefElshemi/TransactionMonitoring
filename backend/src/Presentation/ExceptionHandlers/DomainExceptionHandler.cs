using System.Net.Mime;
using System.Text.Json;
using Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Presentation.Extensions;
using JsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace Presentation.ExceptionHandlers;

public class DomainExceptionHandler(IOptions<JsonOptions>? jsonOptions) : IExceptionHandler
{
    private readonly JsonSerializerOptions _jsonSerializerOptions =
        jsonOptions?.Value.JsonSerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false;
        }

        Tracer.CurrentSpan.SetStatus(Status.Error);
        Tracer.CurrentSpan.RecordException(exception);

        await WriteHttpResponseAsync(httpContext, domainException, cancellationToken);

        return true;
    }

    private Task WriteHttpResponseAsync(
        HttpContext httpContext,
        DomainException exception,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = (int)exception.GetStatusCode();
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;

        var problemDetails = new ProblemDetails
        {
            Type = exception.GetType().Name,
            Title = exception.GetTitle(),
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Status = httpContext.Response.StatusCode
        }.WithContract(httpContext, exception.GetCode());

        AddExtensions(problemDetails, exception.GetExtensions());

        return httpContext.Response.WriteAsJsonAsync(problemDetails, _jsonSerializerOptions, cancellationToken);
    }

    // Public because a test checks extensions never shadow an RFC 7807 member; the handler silently drops ones that do.
    public static readonly IReadOnlySet<string> ReservedMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "type",
        "title",
        "status",
        "detail",
        "instance",
        ProblemDetailsExtensions.Code,
        ProblemDetailsExtensions.CorrelationId
    };

    public static string ToExtensionName(string propertyName)
    {
        return JsonNamingPolicy.CamelCase.ConvertName(propertyName);
    }

    private static void AddExtensions(ProblemDetails problemDetails, object? extensions)
    {
        if (extensions is null)
        {
            return;
        }

        foreach (var property in extensions.GetType().GetProperties())
        {
            if (!property.CanRead)
            {
                continue;
            }

            var name = ToExtensionName(property.Name);

            if (ReservedMembers.Contains(name))
            {
                continue;
            }

            problemDetails.Extensions.TryAdd(name, property.GetValue(extensions));
        }
    }
}
