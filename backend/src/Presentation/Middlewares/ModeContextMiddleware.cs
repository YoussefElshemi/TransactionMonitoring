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

        if (!string.IsNullOrWhiteSpace(requested) && !ModeHeaderHelper.TryParse(requested, out _))
        {
            return RejectAsync(context, requested!);
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ModeConstants.ModeHeader] = ModeHeaderHelper.ToHeaderValue(modeContext.CurrentOrLive);

            return Task.CompletedTask;
        });

        return next(context);
    }

    private static Task RejectAsync(HttpContext context, string requested)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail =
                $"'{requested}' is not a valid {ModeConstants.ModeHeader}. " +
                $"Use '{ModeConstants.Live}' or '{ModeConstants.Test}'.",
            Instance = context.Request.Path
        }.WithContract(context, ErrorCodes.InvalidMode);

        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        return context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }
}
