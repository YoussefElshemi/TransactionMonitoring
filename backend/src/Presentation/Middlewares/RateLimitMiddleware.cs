using System.Globalization;
using Core.Constants;
using Microsoft.AspNetCore.Mvc;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.RateLimiting;

namespace Presentation.Middlewares;

public class RateLimitMiddleware(RequestDelegate next, RequestRateLimiter rateLimiter)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!rateLimiter.IsEnabled || RequestRateLimiter.IsExempt(context))
        {
            await next(context);

            return;
        }

        var outcome = await rateLimiter.AcquireAsync(context);

        WriteHeaders(context, outcome);

        if (outcome.IsAllowed)
        {
            await next(context);

            return;
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = $"Rate limit exceeded. Retry after {outcome.RetryAfterSeconds} seconds.",
            Instance = context.Request.Path
        }.WithContract(context, ErrorCodes.RateLimited);

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }

    private static void WriteHeaders(HttpContext context, RateLimitResult outcome)
    {
        context.Response.Headers[HeaderNames.RateLimitLimit] = ToHeaderValue(outcome.Limit);
        context.Response.Headers[HeaderNames.RateLimitRemaining] = ToHeaderValue(outcome.Remaining);
        context.Response.Headers[HeaderNames.RateLimitReset] = ToHeaderValue(outcome.ResetSeconds);

        if (!outcome.IsAllowed)
        {
            context.Response.Headers[HeaderNames.RetryAfter] = ToHeaderValue(outcome.RetryAfterSeconds);
        }
    }

    private static string ToHeaderValue(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
