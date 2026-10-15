using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Plans;

namespace Presentation.Validators.Plans;

public class UpdatePlanRequestValidator : AbstractValidator<UpdatePlanRequestDto>
{
    public UpdatePlanRequestValidator(IPlanService planService)
    {
        RuleFor(x => x.PlanId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await planService.ExistsAsync(new PlanId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Plan not found");

        RuleFor(x => x.Body.Name)
            .MaximumLength(PlanName.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Name));

        RuleFor(x => x.Body.Description)
            .MaximumLength(PlanDescription.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Description));
    }
}
