using System.Text.Json.Serialization;
using Presentation.Models.Organizations;

namespace Presentation.Models.PartyMappings;

public record PartyMappingResourceDto
{
    public required PartyMappingResponseDto PartyMapping { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}

public record PartyMappingVersionResourceDto
{
    public required PartyMappingVersionResponseDto PartyMappingVersion { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}
