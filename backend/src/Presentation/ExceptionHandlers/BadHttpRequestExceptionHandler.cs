using Core.Constants;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;
using Presentation.Extensions;

namespace Presentation.ExceptionHandlers;

internal sealed class BadHttpRequestExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken stoppingToken)
    {
        if (exception is not BadHttpRequestException badHttpRequestException)
        {
            return false;
        }

        Tracer.CurrentSpan.SetStatus(Status.Error);
        Tracer.CurrentSpan.RecordException(exception);

        var problemDetails = new ValidationProblemDetails
        {
            Type = badHttpRequestException.GetType().Name,
            Title = "BadRequest Error",
            Status = StatusCodes.Status400BadRequest,
            Detail = badHttpRequestException.Message,
            Instance = httpContext.Request.Path,
            Errors = new Dictionary<string, string[]>()
        }.WithContract(httpContext, ErrorCodes.BadRequest);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, stoppingToken);

        return true;
    }
}