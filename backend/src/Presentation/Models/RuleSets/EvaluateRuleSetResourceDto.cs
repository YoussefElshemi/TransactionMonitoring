using System.Text.Json.Serialization;
using Presentation.Models.Rules;
using Presentation.Models.Users;

namespace Presentation.Models.RuleSets;

public record EvaluateRuleSetResourceDto
{
    public required EvaluateRuleSetResponseDto Result { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleSetResponseDto? RuleSet { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleResponseDto[]? Rules { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto[]? Users { get; init; }
}
