using FluentValidation;
using Presentation.Models.Revenue;

namespace Presentation.Validators.Revenue;

public class CreateServiceCostRateRequestValidator : AbstractValidator<CreateServiceCostRateRequestDto>
{
    public CreateServiceCostRateRequestValidator()
    {
        RuleFor(x => x.Body.BillableResource).IsInEnum();
        RuleFor(x => x.Body.Currency).IsInEnum();

        RuleFor(x => x.Body.UnitCost)
            .GreaterThanOrEqualTo(0)
            .WithMessage("A negative cost would read as margin nobody made");
    }
}
