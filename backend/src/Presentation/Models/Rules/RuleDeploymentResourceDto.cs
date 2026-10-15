using System.Text.Json.Serialization;
using Presentation.Models.Users;

namespace Presentation.Models.Rules;

public record RuleDeploymentResourceDto
{
    public required RuleDeploymentResponseDto RuleDeployment { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleResponseDto? Rule { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? CreatedByUser { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleVersionResponseDto? TargetRuleVersion { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleVersionResponseDto? PreviousRuleVersion { get; init; }
}
