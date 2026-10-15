using Core.Models.Invitations;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IInvitationRepository : IPagedRepository<ListInvitationRequest, Invitation>
{
    Task AddAsync(Invitation invitation, Hash tokenHash, CancellationToken stoppingToken);
    Task<Invitation> GetAsync(InvitationId invitationId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, InvitationId invitationId, CancellationToken stoppingToken);

    Task<Invitation?> FindByTokenAsync(Hash tokenHash, CancellationToken stoppingToken);

    Task<Invitation?> FindPendingByEmailAsync(OrganizationId organizationId, Email email, CancellationToken stoppingToken);

    Task ReissueAsync(InvitationId invitationId, Hash tokenHash, ExpiresAt expiresAt, CancellationToken stoppingToken);
    Task AcceptAsync(InvitationId invitationId, ApplicationUserId acceptedByUserId, CancellationToken stoppingToken);
    Task RevokeAsync(InvitationId invitationId, ApplicationUserId revokedByUserId, CancellationToken stoppingToken);

    Task<Count> RevokePendingByInviterAsync(
        OrganizationId organizationId, ApplicationUserId invitedByUserId, ApplicationUserId revokedByUserId, CancellationToken stoppingToken);
}
