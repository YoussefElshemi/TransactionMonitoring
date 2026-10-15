using System.Text.Json.Serialization;
using Presentation.Models.Organizations;

namespace Presentation.Models.Parties;

public record PartyResourceDto
{
    public required PartyResponseDto Party { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}
