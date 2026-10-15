using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Subscriptions;

namespace Presentation.Validators.Subscriptions;

public class DeleteSubscriptionRequestValidator : AbstractValidator<DeleteSubscriptionRequestDto>
{
    public DeleteSubscriptionRequestValidator(
        IOrganizationService organizationService,
        ISubscriptionService subscriptionService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.SubscriptionId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (dto, x, _, ct) =>
                await subscriptionService.ExistsAsync(new OrganizationId(dto.OrganizationId), new SubscriptionId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Subscription not found");
    }
}
