using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Plans;

namespace Presentation.Validators.Plans;

public class PublishPlanVersionRequestValidator : AbstractValidator<PublishPlanVersionRequestDto>
{
    public PublishPlanVersionRequestValidator(
        IPlanService planService,
        IValidator<PlanPriceRequestDto> planPriceRequestValidator)
    {
        RuleFor(x => x.PlanId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await planService.ExistsAsync(new PlanId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Plan not found");

        RuleFor(x => x.Body.CostPerRequest)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Body.PlatformFee)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Body.MinimumCharge)
            .GreaterThanOrEqualTo(0);

        RuleForEach(x => x.Body.Prices)
            .SetValidator(planPriceRequestValidator);
    }
}
