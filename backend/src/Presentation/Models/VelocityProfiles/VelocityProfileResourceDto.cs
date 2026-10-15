using System.Text.Json.Serialization;
using Presentation.Models.Organizations;

namespace Presentation.Models.VelocityProfiles;

public record VelocityProfileResourceDto
{
    public required VelocityProfileResponseDto VelocityProfile { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}

public record VelocityBackfillResourceDto
{
    public required VelocityBackfillResponseDto VelocityBackfill { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}
