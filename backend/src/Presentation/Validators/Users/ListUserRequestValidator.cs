using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.Users;
using Presentation.Models.Users;
using Presentation.Validators.Common;

namespace Presentation.Validators.Users;

public class ListUserRequestValidator(
    IApplicationUserService applicationUserService,
    TimeProvider timeProvider) : PagedRequestValidator<ListUserRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListUserRequestDto request, CancellationToken stoppingToken)
    {
        return applicationUserService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}