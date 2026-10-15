using System.Text.Json.Serialization;
using Presentation.Models.Rules;
using Presentation.Models.Users;

namespace Presentation.Models.Decisions;

public record DecisionResourceDto
{
    public required DecisionResponseDto Decision { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? ResolvedByUser { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleResponseDto[]? Rules { get; init; }
}
