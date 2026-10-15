using Core.Helpers;
using Core.Models.Invitations;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class InvitationMapper
{
    public static Invitation ToModel(this InvitationEntity invitationEntity)
    {
        return new Invitation
        {
            InvitationId = new InvitationId(invitationEntity.InvitationId),
            OrganizationId = new OrganizationId(invitationEntity.OrganizationId),
            Email = new Email(invitationEntity.Email),
            UserRole = RoleHelper.MapToUserRole(invitationEntity.RoleId),
            SubOrganizationId = invitationEntity.SubOrganizationId.HasValue
                ? new SubOrganizationId(invitationEntity.SubOrganizationId.Value)
                : null,
            InvitedByUserId = new ApplicationUserId(invitationEntity.InvitedByUserId),
            ExpiresAt = new ExpiresAt(invitationEntity.ExpiresAt),
            AcceptedAt = invitationEntity.AcceptedAt.HasValue ? new AcceptedAt(invitationEntity.AcceptedAt.Value) : null,
            AcceptedByUserId = invitationEntity.AcceptedByUserId.HasValue
                ? new ApplicationUserId(invitationEntity.AcceptedByUserId.Value)
                : null,
            RevokedAt = invitationEntity.RevokedAt.HasValue ? new RevokedAt(invitationEntity.RevokedAt.Value) : null,
            RevokedByUserId = invitationEntity.RevokedByUserId.HasValue
                ? new ApplicationUserId(invitationEntity.RevokedByUserId.Value)
                : null,
            CreatedAt = new CreatedAt(invitationEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(invitationEntity.UpdatedAt)
        };
    }

    public static InvitationEntity ToEntity(this Invitation invitation, Hash tokenHash)
    {
        return new InvitationEntity
        {
            InvitationId = invitation.InvitationId,
            OrganizationId = invitation.OrganizationId,
            Email = invitation.Email,
            NormalizedEmail = ((string)invitation.Email).Trim().ToUpperInvariant(),
            RoleId = RoleHelper.MapFromUserRole(invitation.UserRole),
            SubOrganizationId = invitation.SubOrganizationId.HasValue ? (Guid)invitation.SubOrganizationId.Value : null,
            InvitedByUserId = invitation.InvitedByUserId,
            TokenHash = tokenHash,
            ExpiresAt = invitation.ExpiresAt,
            AcceptedAt = invitation.AcceptedAt.HasValue ? (DateTimeOffset)invitation.AcceptedAt.Value : null,
            AcceptedByUserId = invitation.AcceptedByUserId.HasValue ? (Guid)invitation.AcceptedByUserId.Value : null,
            RevokedAt = invitation.RevokedAt.HasValue ? (DateTimeOffset)invitation.RevokedAt.Value : null,
            RevokedByUserId = invitation.RevokedByUserId.HasValue ? (Guid)invitation.RevokedByUserId.Value : null,
            CreatedAt = invitation.CreatedAt,
            UpdatedAt = invitation.UpdatedAt
        };
    }
}
