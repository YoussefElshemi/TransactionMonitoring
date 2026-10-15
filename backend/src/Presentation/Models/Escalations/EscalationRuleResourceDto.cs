using System.Text.Json.Serialization;
using Presentation.Models.Organizations;

namespace Presentation.Models.Escalations;

public record EscalationRuleResourceDto
{
    public required EscalationRuleResponseDto EscalationRule { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}
