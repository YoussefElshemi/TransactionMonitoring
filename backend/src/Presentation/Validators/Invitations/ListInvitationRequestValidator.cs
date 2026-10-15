using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.Invitations;
using Presentation.Models.Invitations;
using Presentation.Validators.Common;

namespace Presentation.Validators.Invitations;

public class ListInvitationRequestValidator(
    IInvitationService invitationService,
    TimeProvider timeProvider) : PagedRequestValidator<ListInvitationRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListInvitationRequestDto request, CancellationToken stoppingToken)
    {
        return invitationService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
