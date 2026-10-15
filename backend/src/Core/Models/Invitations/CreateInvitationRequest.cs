using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Invitations;

public record CreateInvitationRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required Email Email { get; init; }

    public required UserRole UserRole { get; init; }
    public required SubOrganizationId? SubOrganizationId { get; init; }
    public required ApplicationUserId InvitedByUserId { get; init; }
}
