using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Plans;
using Presentation.Models.Plans;
using Presentation.Validators.Common;

namespace Presentation.Validators.Plans;

public class ListPlanVersionRequestValidator(
    IPlanService planService,
    TimeProvider timeProvider) : PagedRequestValidator<ListPlanVersionRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.PlanId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await planService.ExistsAsync(new PlanId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Plan not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListPlanVersionRequestDto request, CancellationToken stoppingToken)
    {
        return planService.GetVersionCountAsync(request.ToModel(), stoppingToken);
    }
}
