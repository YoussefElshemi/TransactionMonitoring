using System.Text.Json.Serialization;
using Presentation.Models.Organizations;
using Presentation.Models.Users;

namespace Presentation.Models.RuleSets;

public record RuleSetResourceDto
{
    public required RuleSetResponseDto RuleSet { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? CreatedByUser { get; init; }
}
