using Core.Models.Invitations;

namespace Core.Interfaces.Services;

public interface IInvitationNotifier
{
    Task NotifyAsync(IssuedInvitation issuedInvitation, CancellationToken stoppingToken);
}
