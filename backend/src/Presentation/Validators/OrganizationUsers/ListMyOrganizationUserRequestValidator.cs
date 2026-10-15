using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.OrganizationUsers;
using Presentation.Models.OrganizationUsers;
using Presentation.Validators.Common;

namespace Presentation.Validators.OrganizationUsers;

public class ListMyOrganizationUserRequestValidator(
    IOrganizationUserService organizationUserService,
    TimeProvider timeProvider) : PagedRequestValidator<ListMyOrganizationUserRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListMyOrganizationUserRequestDto request, CancellationToken stoppingToken)
    {
        return organizationUserService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
