using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.NotificationSubscriptions;

namespace Presentation.Validators.NotificationSubscriptions;

public class DeleteNotificationSubscriptionRequestValidator : AbstractValidator<DeleteNotificationSubscriptionRequestDto>
{
    public DeleteNotificationSubscriptionRequestValidator(INotificationSubscriptionService notificationSubscriptionService)
    {
        RuleFor(x => x.NotificationSubscriptionId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await notificationSubscriptionService.ExistsAsync(new OrganizationId(x.OrganizationId), new NotificationSubscriptionId(x.NotificationSubscriptionId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Notification Subscription not found");
    }
}
