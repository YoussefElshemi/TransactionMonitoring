using System.Security.Claims;
using Core.Interfaces.Services;
using Core.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Presentation.Extensions;

namespace Presentation.Middlewares;

public class EnsureUserExistsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IApplicationUserService applicationUserService, IApiClientService apiClientService)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<AuthorizeAttribute>() != null)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            if (context.User.IsApiClient())
            {
                var isActive = await apiClientService.IsActiveAsync(
                    new ApiClientId(context.User.GetApiClientId()), context.RequestAborted);

                if (!isActive)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }
            else
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userId, out var applicationUserId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                var exists = await applicationUserService.ExistsAsync(
                    new ApplicationUserId(applicationUserId), new IncludeDeleted(false), context.RequestAborted);

                if (!exists)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }
        }

        await next(context);
    }
}
