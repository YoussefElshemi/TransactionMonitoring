using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Plans;

namespace Presentation.Validators.Plans;

public class DeletePlanRequestValidator : AbstractValidator<DeletePlanRequestDto>
{
    public DeletePlanRequestValidator(IPlanService planService)
    {
        RuleFor(x => x.PlanId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await planService.ExistsAsync(new PlanId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Plan not found");
    }
}
