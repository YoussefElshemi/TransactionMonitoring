using System.Text.Json.Serialization;
using Presentation.Models.Organizations;

namespace Presentation.Models.Users;

public record UserResourceDto
{
    public required UserResponseDto User { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}
