using Core.Models.Users;
using Core.Models.Webhooks;

namespace Core.Mappers.Webhooks;

public static class UserWebhookMessageMapper
{
    public static UserWebhookMessage ToWebhook(this ApplicationUser applicationUser)
    {
        return new UserWebhookMessage
        {
            UserId = applicationUser.UserId,
            Email = applicationUser.Email,
            UserName = applicationUser.UserName
        };
    }
}