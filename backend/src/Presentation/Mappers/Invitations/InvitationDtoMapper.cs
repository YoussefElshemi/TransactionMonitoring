using Core.Helpers;
using Core.Models.Invitations;
using Core.Models.Organizations;
using Presentation.Models.Invitations;

namespace Presentation.Mappers.Invitations;

public static class InvitationDtoMapper
{
    public static InvitationResponseDto ToDto(
        this Invitation invitation, DateTimeOffset now, bool invitedByPlatformOperator = false)
    {
        return new InvitationResponseDto
        {
            InvitationId = invitation.InvitationId,
            OrganizationId = invitation.OrganizationId,
            Email = invitation.Email,
            Role = invitation.UserRole.ToString(),
            SubOrganizationId = invitation.SubOrganizationId.HasValue ? (Guid)invitation.SubOrganizationId.Value : null,
            InvitedByUserId = invitation.InvitedByUserId,
            InvitedByPlatformOperator = invitedByPlatformOperator,
            Status = InvitationStatusResolver.Resolve(invitation, now).ToString(),
            ExpiresAt = invitation.ExpiresAt,
            AcceptedAt = invitation.AcceptedAt.HasValue ? (DateTimeOffset)invitation.AcceptedAt.Value : null,
            AcceptedByUserId = invitation.AcceptedByUserId.HasValue ? (Guid)invitation.AcceptedByUserId.Value : null,
            RevokedAt = invitation.RevokedAt.HasValue ? (DateTimeOffset)invitation.RevokedAt.Value : null,
            RevokedByUserId = invitation.RevokedByUserId.HasValue ? (Guid)invitation.RevokedByUserId.Value : null,
            CreatedAt = invitation.CreatedAt,
            UpdatedAt = invitation.UpdatedAt
        };
    }

    public static IssuedInvitationResponseDto ToDto(
        this IssuedInvitation issuedInvitation, string acceptUrl, DateTimeOffset now, bool invitedByPlatformOperator = false)
    {
        var invitation = issuedInvitation.Invitation;
        var invitationToken = (string)issuedInvitation.InvitationToken;

        return new IssuedInvitationResponseDto
        {
            InvitationId = invitation.InvitationId,
            OrganizationId = invitation.OrganizationId,
            Email = invitation.Email,
            Role = invitation.UserRole.ToString(),
            SubOrganizationId = invitation.SubOrganizationId.HasValue ? (Guid)invitation.SubOrganizationId.Value : null,
            InvitedByUserId = invitation.InvitedByUserId,
            InvitedByPlatformOperator = invitedByPlatformOperator,
            Status = InvitationStatusResolver.Resolve(invitation, now).ToString(),
            ExpiresAt = invitation.ExpiresAt,
            AcceptedAt = null,
            AcceptedByUserId = null,
            RevokedAt = null,
            RevokedByUserId = null,
            CreatedAt = invitation.CreatedAt,
            UpdatedAt = invitation.UpdatedAt,
            InvitationToken = invitationToken,
            AcceptUrl = InvitationLinkBuilder.Build(acceptUrl, issuedInvitation.InvitationToken)
        };
    }

    public static InvitationPreviewResponseDto ToPreviewDto(this Invitation invitation, Organization organization)
    {
        return new InvitationPreviewResponseDto
        {
            Email = invitation.Email,
            OrganizationName = organization.FriendlyName,
            Role = invitation.UserRole.ToString(),
            ExpiresAt = invitation.ExpiresAt
        };
    }

    public static AcceptedInvitationResponseDto ToDto(this AcceptedInvitation acceptedInvitation)
    {
        return new AcceptedInvitationResponseDto
        {
            UserId = acceptedInvitation.UserId,
            OrganizationId = acceptedInvitation.Invitation.OrganizationId,
            Role = acceptedInvitation.Invitation.UserRole.ToString(),
            IsCreated = acceptedInvitation.IsCreated
        };
    }
}
