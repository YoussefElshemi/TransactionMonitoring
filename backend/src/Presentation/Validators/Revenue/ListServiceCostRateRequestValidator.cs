using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Revenue;
using Presentation.Models.Revenue;
using Presentation.Validators.Common;

namespace Presentation.Validators.Revenue;

public class ListServiceCostRateRequestValidator(
    IServiceCostRateService serviceCostRateService,
    TimeProvider timeProvider) : PagedRequestValidator<ListServiceCostRateRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.BillableResource).IsInEnum().When(x => x.BillableResource.HasValue);
    }

    protected override Task<TotalCount> GetCountAsync(ListServiceCostRateRequestDto request, CancellationToken stoppingToken)
    {
        return serviceCostRateService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
