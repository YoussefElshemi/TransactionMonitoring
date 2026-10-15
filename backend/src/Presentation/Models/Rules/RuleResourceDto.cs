using System.Text.Json.Serialization;
using Presentation.Models.Organizations;
using Presentation.Models.RuleSets;
using Presentation.Models.Users;

namespace Presentation.Models.Rules;

public record RuleResourceDto
{
    public required RuleResponseDto Rule { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleSetResponseDto? RuleSet { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? CreatedByUser { get; init; }
}
