using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Subscriptions;

namespace Presentation.Validators.Subscriptions;

public class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequestDto>
{
    public CreateSubscriptionRequestValidator(
        IOrganizationService organizationService,
        IPlanService planService,
        TimeProvider timeProvider)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.PlanVersionId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await planService.VersionExistsAsync(new PlanVersionId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Plan version not found");

        RuleFor(x => x.Body.EffectiveFrom)
            .Must(x => x!.Value > timeProvider.GetUtcNow())
            .WithMessage("EffectiveFrom must be in the future. Omit it to start the subscription now.")
            .When(x => x.Body.EffectiveFrom.HasValue);
    }
}
