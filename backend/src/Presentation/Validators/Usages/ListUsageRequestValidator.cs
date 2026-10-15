using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.Usages;
using Presentation.Models.Usages;
using Presentation.Validators.Common;

namespace Presentation.Validators.Usages;

public class ListUsageRequestValidator(
    IUsageService usageService,
    TimeProvider timeProvider) : PagedRequestValidator<ListUsageRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListUsageRequestDto request, CancellationToken stoppingToken)
    {
        return usageService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}