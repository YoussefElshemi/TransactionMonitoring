using Core.Constants;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;
using Presentation.Extensions;
using ILogger = Serilog.ILogger;

namespace Presentation.ExceptionHandlers;

internal sealed class GlobalExceptionHandler(ILogger logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken stoppingToken)
    {
        Tracer.CurrentSpan.SetStatus(Status.Error);
        Tracer.CurrentSpan.RecordException(exception);
        logger.Error(exception, "Exception occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Detail = "An unexpected error occurred.",
            Title = "Internal Server Error",
            Instance = httpContext.Request.Path
        }.WithContract(httpContext, ErrorCodes.InternalError);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, stoppingToken);

        return true;
    }
}