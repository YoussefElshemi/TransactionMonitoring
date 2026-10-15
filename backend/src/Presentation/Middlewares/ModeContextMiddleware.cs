using Core.Constants;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Presentation.Constants;
using Presentation.Extensions;

namespace Presentation.Middlewares;

// Only validates and echoes; the mode itself resolves lazily because the API client token isn't validated yet here.
public class ModeContextMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, IModeContext modeContext)
    {
        var requested = context.Request.Headers[ModeConstants.ModeHeader];

        if (!string.IsNullOrWhiteSpace(requested))
        {
            if (!ModeHeaderHelper.TryParse(requested, out var requestedMode))
            {
                return RejectInvalidAsync(context, requested!);
            }

            // Relies on UseAuthorization running first: the API-scope policies are what authenticate the client scheme.
            if (context.User.IsApiClient() && context.User.GetMode() != requestedMode)
            {
                return RejectMismatchAsync(context, ModeHeaderHelper.ToHeaderValue(requestedMode));
            }
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ModeConstants.ModeHeader] = ModeHeaderHelper.ToHeaderValue(modeContext.CurrentOrLive);

            return Task.CompletedTask;
        });

        return next(context);
    }

    private static Task RejectInvalidAsync(HttpContext context, string requested)
    {
        return RejectAsync(context, StatusCodes.Status400BadRequest, "Bad Request",
            $"'{requested}' is not a valid {ModeConstants.ModeHeader}. " +
            $"Use '{ModeConstants.Live}' or '{ModeConstants.Test}'.",
            ErrorCodes.InvalidMode);
    }

    private static Task RejectMismatchAsync(HttpContext context, string requested)
    {
        var issued = ModeHeaderHelper.ToHeaderValue(context.User.GetMode());

        return RejectAsync(context, StatusCodes.Status403Forbidden, "Forbidden",
            $"This API client's key was issued in '{issued}' mode and cannot be used for '{requested}'. " +
            $"Use a {requested} key, or omit {ModeConstants.ModeHeader}.",
            ErrorCodes.ModeMismatch);
    }

    private static Task RejectAsync(HttpContext context, int statusCode, string title, string detail, string errorCode)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        }.WithContract(context, errorCode);

        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }
}
