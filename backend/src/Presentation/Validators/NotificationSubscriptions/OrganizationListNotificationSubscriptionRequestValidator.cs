using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.NotificationSubscriptions;
using Presentation.Models.NotificationSubscriptions;
using Presentation.Validators.Common;

namespace Presentation.Validators.NotificationSubscriptions;

public class OrganizationListNotificationSubscriptionRequestValidator(
    INotificationSubscriptionService notificationSubscriptionService,
    IOrganizationService organizationService,
    TimeProvider timeProvider) : PagedRequestValidator<OrganizationListNotificationSubscriptionRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(
        OrganizationListNotificationSubscriptionRequestDto request,
        CancellationToken stoppingToken)
    {
        return notificationSubscriptionService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
