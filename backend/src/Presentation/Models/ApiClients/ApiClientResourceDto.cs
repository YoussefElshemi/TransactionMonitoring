using System.Text.Json.Serialization;
using Presentation.Models.Organizations;
using Presentation.Models.Users;

namespace Presentation.Models.ApiClients;

public record ApiClientResourceDto
{
    public required ApiClientResponseDto ApiClient { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? CreatedByUser { get; init; }
}

// STJ serializes the declared type, so the one-time secret needs its own resource type.
public record CreatedApiClientResourceDto
{
    public required CreatedApiClientResponseDto ApiClient { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? CreatedByUser { get; init; }
}
