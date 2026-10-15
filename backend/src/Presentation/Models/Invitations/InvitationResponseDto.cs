namespace Presentation.Models.Invitations;

public record InvitationResponseDto
{
    public required Guid InvitationId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public required Guid? SubOrganizationId { get; init; }
    public required Guid InvitedByUserId { get; init; }

    public required bool InvitedByPlatformOperator { get; init; }

    public required string Status { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required DateTimeOffset? AcceptedAt { get; init; }
    public required Guid? AcceptedByUserId { get; init; }
    public required DateTimeOffset? RevokedAt { get; init; }
    public required Guid? RevokedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public record IssuedInvitationResponseDto : InvitationResponseDto
{
    public required string InvitationToken { get; init; }
    public required string AcceptUrl { get; init; }
}

public record InvitationPreviewResponseDto
{
    public required string Email { get; init; }
    public required string OrganizationName { get; init; }
    public required string Role { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}

public record AcceptedInvitationResponseDto
{
    public required Guid UserId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Role { get; init; }

    public required bool IsCreated { get; init; }
}
