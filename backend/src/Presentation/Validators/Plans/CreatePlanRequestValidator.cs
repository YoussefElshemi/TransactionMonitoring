using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Plans;

namespace Presentation.Validators.Plans;

public class CreatePlanRequestValidator : AbstractValidator<CreatePlanRequestDto>
{
    public CreatePlanRequestValidator(
        IOrganizationService organizationService,
        IPlanService planService,
        IValidator<PlanPriceRequestDto> planPriceRequestValidator)
    {
        RuleFor(x => x.Body.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(PlanName.MaxLength)
            .MustAsync(async (dto, x, _, ct) => !await planService.ExistsByNameAsync(
                new PlanName(x),
                dto.Body.OrganizationId.HasValue ? new OrganizationId(dto.Body.OrganizationId.Value) : null,
                ct))
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithMessage("A plan with that name already exists");

        RuleFor(x => x.Body.Description)
            .MaximumLength(PlanDescription.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Description));

        RuleFor(x => x.Body.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .When(x => x.Body.OrganizationId.HasValue);

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
