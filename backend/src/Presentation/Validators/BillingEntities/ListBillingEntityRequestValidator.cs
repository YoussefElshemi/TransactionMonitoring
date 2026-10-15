using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.BillingEntities;
using Presentation.Models.BillingEntities;
using Presentation.Validators.Common;

namespace Presentation.Validators.BillingEntities;

public class ListBillingEntityRequestValidator(
    IBillingEntityService billingEntityService,
    TimeProvider timeProvider) : PagedRequestValidator<ListBillingEntityRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListBillingEntityRequestDto request, CancellationToken stoppingToken)
    {
        return billingEntityService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
