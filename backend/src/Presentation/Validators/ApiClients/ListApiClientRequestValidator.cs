using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.ApiClients;
using Presentation.Models.ApiClients;
using Presentation.Validators.Common;

namespace Presentation.Validators.ApiClients;

public class ListApiClientRequestValidator(
    IApiClientService apiClientService,
    TimeProvider timeProvider) : PagedRequestValidator<ListApiClientRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListApiClientRequestDto request, CancellationToken stoppingToken)
    {
        return apiClientService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
