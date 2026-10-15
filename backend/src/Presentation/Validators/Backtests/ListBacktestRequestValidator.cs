using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.BackTests;
using Presentation.Models.BackTests;
using Presentation.Validators.Common;

namespace Presentation.Validators.BackTests;

public class ListBackTestRequestValidator(
    IBackTestService backTestService,
    TimeProvider timeProvider) : PagedRequestValidator<ListBackTestRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListBackTestRequestDto request, CancellationToken stoppingToken)
    {
        return backTestService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
