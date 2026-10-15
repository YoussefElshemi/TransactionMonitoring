using System.Text.Json.Serialization;
using Presentation.Models.Users;

namespace Presentation.Models.Rules;

public record EvaluateRuleResourceDto
{
    public required EvaluateRuleResponseDto Result { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleResponseDto[]? Rules { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto[]? Users { get; init; }
}
