using System.Text.Json.Serialization;
using Presentation.Models.Users;

namespace Presentation.Models.Rules;

public record EvaluateAllRulesResourceDto
{
    public required EvaluateAllRulesResponseDto Result { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleResponseDto[]? Rules { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto[]? Users { get; init; }
}
