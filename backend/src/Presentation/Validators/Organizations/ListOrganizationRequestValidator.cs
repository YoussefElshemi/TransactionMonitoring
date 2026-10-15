using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.Organizations;
using Presentation.Models.Organizations;
using Presentation.Validators.Common;

namespace Presentation.Validators.Organizations;

public class ListOrganizationRequestValidator(
    IOrganizationService organizationService,
    TimeProvider timeProvider) : PagedRequestValidator<ListOrganizationRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListOrganizationRequestDto request, CancellationToken stoppingToken)
    {
        return organizationService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}