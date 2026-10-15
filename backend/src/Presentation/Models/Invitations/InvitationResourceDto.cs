using System.Text.Json.Serialization;
using Presentation.Models.Organizations;
using Presentation.Models.Users;

namespace Presentation.Models.Invitations;

public record InvitationResourceDto
{
    public required InvitationResponseDto Invitation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? InvitedByUser { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? AcceptedByUser { get; init; }
}

// STJ serializes the declared type, so the one-time token needs its own resource type.
public record IssuedInvitationResourceDto
{
    public required IssuedInvitationResponseDto Invitation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? InvitedByUser { get; init; }
}
