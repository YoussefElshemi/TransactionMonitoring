using Core.Models.Invitations;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IInvitationService : IPagedService<ListInvitationRequest, Invitation>
{
    Task<IssuedInvitation> InviteAsync(CreateInvitationRequest createInvitationRequest, CancellationToken stoppingToken);
    Task<Invitation> GetAsync(InvitationId invitationId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, InvitationId invitationId, CancellationToken stoppingToken);
    Task<IssuedInvitation> ResendAsync(InvitationId invitationId, CancellationToken stoppingToken);
    Task RevokeAsync(InvitationId invitationId, ApplicationUserId revokedByUserId, CancellationToken stoppingToken);

    Task<Invitation> PreviewAsync(InvitationToken invitationToken, CancellationToken stoppingToken);

    Task<AcceptedInvitation> AcceptAsync(AcceptInvitationRequest acceptInvitationRequest, CancellationToken stoppingToken);
}
