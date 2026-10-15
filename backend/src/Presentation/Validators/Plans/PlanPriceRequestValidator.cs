using FluentValidation;
using Presentation.Models.Plans;

namespace Presentation.Validators.Plans;

public class PlanPriceRequestValidator : AbstractValidator<PlanPriceRequestDto>
{
    public PlanPriceRequestValidator()
    {
        RuleFor(x => x.BillableResource)
            .IsInEnum()
            .When(x => x.BillableResource.HasValue);

        RuleFor(x => x.PricingModel)
            .IsInEnum();

        RuleFor(x => x.FreeAllowance)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Tiers)
            .NotEmpty()
            .WithMessage("A price needs at least one band");

        RuleForEach(x => x.Tiers).ChildRules(tier =>
        {
            tier.RuleFor(x => x.FromQuantity)
                .GreaterThanOrEqualTo(0);

            tier.RuleFor(x => x.ToQuantity)
                .GreaterThan(x => x.FromQuantity)
                .When(x => x.ToQuantity.HasValue)
                .WithMessage("A band cannot end at or before it starts");

            tier.RuleFor(x => x.UnitAmount)
                .GreaterThanOrEqualTo(0);

            tier.RuleFor(x => x.FlatAmount)
                .GreaterThanOrEqualTo(0);
        });
    }
}
