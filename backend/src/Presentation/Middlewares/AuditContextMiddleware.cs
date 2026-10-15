using System.Security.Claims;
using Core.Interfaces;
using Presentation.Extensions;

namespace Presentation.Middlewares;

public class AuditContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAuditContext auditContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            if (context.User.IsApiClient())
            {
                auditContext.ActingApiClientId = context.User.GetApiClientId();
            }
            else if (Guid.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var actingUserId))
            {
                auditContext.ActingUserId = actingUserId;
            }
        }

        await next(context);
    }
}
