using Core.Constants;
using Presentation.Constants;
using Serilog.Context;

namespace Presentation.Middlewares;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString();

        context.TraceIdentifier = correlationId;

        // On response start: the exception handler clears headers before writing its body.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderNames.CorrelationId] = correlationId;

            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(LogPropertyNames.CorrelationId, correlationId))
        {
            await next(context);
        }
    }
}
