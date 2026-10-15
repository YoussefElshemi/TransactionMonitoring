using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Plans;
using Presentation.Models.Plans;
using Presentation.Validators.Common;

namespace Presentation.Validators.Plans;

public class ListPlanRequestValidator(
    IOrganizationService organizationService,
    IPlanService planService,
    TimeProvider timeProvider) : PagedRequestValidator<ListPlanRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.Name)
            .MaximumLength(PlanName.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.OwnerOrganizationId)
            .Cascade(CascadeMode.Stop)
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .When(x => x.OwnerOrganizationId.HasValue);
    }

    protected override Task<TotalCount> GetCountAsync(ListPlanRequestDto request, CancellationToken stoppingToken)
    {
        return planService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
