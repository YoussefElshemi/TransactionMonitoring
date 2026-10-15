using System.Net.Mime;
using System.Text.Json;
using Core.Constants;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.IdempotencyRecords;
using Core.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Presentation.Constants;
using Presentation.Extensions;
using JsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace Presentation.Attributes;

public class IdempotentAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (!TryGetIdempotencyKey(context, out var idempotencyKey))
        {
            return;
        }

        if (idempotencyKey == null)
        {
            await next();

            return;
        }

        var idempotencyService = httpContext.RequestServices.GetRequiredService<IIdempotencyService>();

        var applicationUserService = httpContext.RequestServices.GetRequiredService<IApplicationUserService>();
        var organizationId = await applicationUserService.ResolveOrganizationIdAsync(httpContext.User, httpContext.RequestAborted);
        var resource = new Resource($"{httpContext.Request.Method} {httpContext.Request.Path}");

        var reservation = await idempotencyService.ReserveAsync(new CreateIdempotencyRecordRequest
        {
            IdempotencyKey = idempotencyKey.Value,
            OrganizationId = organizationId,
            Resource = resource
        }, httpContext.RequestAborted);

        switch (reservation.Outcome)
        {
            case IdempotencyOutcome.Replay:
                httpContext.Response.Headers[HeaderNames.IdempotentReplay] = "true";
                context.Result = new ContentResult
                {
                    Content = reservation.Record!.ResponseBody,
                    ContentType = MediaTypeNames.Application.Json,
                    StatusCode = reservation.Record.StatusCode
                };

                return;

            case IdempotencyOutcome.InProgress:
                context.Result = Conflict(
                    httpContext,
                    ErrorCodes.IdempotencyRequestInProgress,
                    "Request in progress",
                    $"An earlier request with this {HeaderNames.IdempotencyKey} is still running. Retry once it has finished.");

                return;

            case IdempotencyOutcome.ResourceMismatch:
                context.Result = Conflict(
                    httpContext,
                    ErrorCodes.IdempotencyKeyReused,
                    $"{HeaderNames.IdempotencyKey} already used",
                    $"This {HeaderNames.IdempotencyKey} was used for a different endpoint. Use a new key for a new request.");

                return;
        }

        var executedContext = await next();
        var (statusCode, value) = ResolveResponse(executedContext.Result);

        if (executedContext.Exception != null || statusCode is < 200 or > 299)
        {
            await idempotencyService.ReleaseAsync(organizationId, idempotencyKey.Value, CancellationToken.None);

            return;
        }

        var jsonSerializerOptions = httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
        var responseBody = value == null ? string.Empty : JsonSerializer.Serialize(value, jsonSerializerOptions);

        await idempotencyService.CompleteAsync(
            organizationId, idempotencyKey.Value, new StatusCode(statusCode), new ResponseBody(responseBody), CancellationToken.None);
    }

    private static bool TryGetIdempotencyKey(ActionExecutingContext context, out IdempotencyKey? idempotencyKey)
    {
        idempotencyKey = null;

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderNames.IdempotencyKey, out var headerValues))
        {
            return true;
        }

        var headerValue = headerValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return true;
        }

        if (!Guid.TryParse(headerValue, out var value))
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Title = $"Invalid {HeaderNames.IdempotencyKey}",
                Detail = $"{HeaderNames.IdempotencyKey} must be a GUID.",
                Status = StatusCodes.Status400BadRequest
            }.WithContract(context.HttpContext, ErrorCodes.InvalidIdempotencyKey));

            return false;
        }

        idempotencyKey = new IdempotencyKey(value);

        return true;
    }

    private static (int StatusCode, object? Value) ResolveResponse(IActionResult? actionResult)
    {
        return actionResult switch
        {
            ObjectResult objectResult => (objectResult.StatusCode ?? StatusCodes.Status200OK, objectResult.Value),
            StatusCodeResult statusCodeResult => (statusCodeResult.StatusCode, null),
            null => (StatusCodes.Status500InternalServerError, null),
            _ => (StatusCodes.Status200OK, null)
        };
    }

    private static ObjectResult Conflict(HttpContext httpContext, string code, string title, string detail)
    {
        return new ObjectResult(new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = StatusCodes.Status409Conflict
        }.WithContract(httpContext, code))
        {
            StatusCode = StatusCodes.Status409Conflict
        };
    }
}
