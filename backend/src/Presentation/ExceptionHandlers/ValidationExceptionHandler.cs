using System.Net;
using System.Net.Mime;
using Core.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;
using Presentation.Extensions;

namespace Presentation.ExceptionHandlers;

internal sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken stoppingToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        Tracer.CurrentSpan.SetStatus(Status.Error);
        Tracer.CurrentSpan.RecordException(exception);

        var validStatusCode = validationException.Errors
            .Select(error => error?.ErrorCode)
            .FirstOrDefault(errorCode => Enum.TryParse(errorCode, true, out HttpStatusCode _));

        if (validStatusCode != null && Enum.TryParse(validStatusCode, true, out HttpStatusCode statusCode))
        {
            await WriteErrorHttpResponseAsync(httpContext, validationException, statusCode, stoppingToken);
        }
        else
        {
            await WriteValidationHttpResponseAsync(httpContext, validationException, stoppingToken);
        }

        return true;
    }

    private static Task WriteValidationHttpResponseAsync(HttpContext httpContext, ValidationException exception, CancellationToken stoppingToken)
    {
        httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;

        return httpContext.Response.WriteAsJsonAsync(
            new ValidationProblemDetails
            {
                Type = exception.GetType().Name,
                Title = HttpStatusCode.BadRequest.ToString(),
                Detail = "Please refer to the errors property for additional details.",
                Instance = httpContext.Request.Path,
                Status = httpContext.Response.StatusCode,
                Errors = exception.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(x => x.Key, x => x.Select(y => y.ErrorMessage).ToArray())
            }.WithContract(httpContext, ErrorCodes.ValidationFailed), stoppingToken);
    }

    private static Task WriteErrorHttpResponseAsync(HttpContext httpContext, ValidationException exception, HttpStatusCode statusCode, CancellationToken stoppingToken)
    {
        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;

        var failure = exception.Errors.FirstOrDefault(x => x.ErrorCode == statusCode.ToString());

        var problemDetails = new ProblemDetails
        {
            Type = statusCode.ToString(),
            Title = statusCode.ToString(),
            Detail = failure?.ErrorMessage,
            Instance = httpContext.Request.Path,
            Status = httpContext.Response.StatusCode
        }.WithContract(httpContext, ErrorCodes.FromStatusCode((int)statusCode));

        if (failure?.CustomState is IReadOnlyDictionary<string, object> extensions)
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions[extension.Key] = extension.Value;
            }
        }

        return httpContext.Response.WriteAsJsonAsync(problemDetails, stoppingToken);
    }
}