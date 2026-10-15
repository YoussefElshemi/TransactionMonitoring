using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Subscriptions;
using Presentation.Models.Subscriptions;
using Presentation.Validators.Common;

namespace Presentation.Validators.Subscriptions;

public class ListSubscriptionRequestValidator(
    IOrganizationService organizationService,
    ISubscriptionService subscriptionService,
    TimeProvider timeProvider) : PagedRequestValidator<ListSubscriptionRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListSubscriptionRequestDto request, CancellationToken stoppingToken)
    {
        return subscriptionService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
