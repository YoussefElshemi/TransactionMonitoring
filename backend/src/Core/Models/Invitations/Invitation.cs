using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Invitations;

public record Invitation : BaseModel
{
    public required InvitationId InvitationId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required Email Email { get; init; }

    public required UserRole UserRole { get; init; }
    public required SubOrganizationId? SubOrganizationId { get; init; }
    public required ApplicationUserId InvitedByUserId { get; init; }
    public required ExpiresAt ExpiresAt { get; init; }
    public required AcceptedAt? AcceptedAt { get; init; }
    public required ApplicationUserId? AcceptedByUserId { get; init; }
    public required RevokedAt? RevokedAt { get; init; }
    public required ApplicationUserId? RevokedByUserId { get; init; }
}
