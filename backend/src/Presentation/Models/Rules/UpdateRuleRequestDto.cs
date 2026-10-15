using System.Text.Json.Serialization;
using Core.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Rules;

public record UpdateRuleRequestDto : RuleRequestDto
{
    [FromBody]
    public required UpdateRuleRequestBodyDto Body { get; init; }
}

public record UpdateRuleRequestBodyDto
{
    public string? FriendlyName { get; init; }
    public bool? IsEnabled { get; init; }
    public string? Action { get; init; }
    public string? Severity { get; init; }
    public int? RiskWeight { get; init; }
    // Omitted when unset so a round-tripped body doesn't turn "leave alone" into "clear".
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<int?> HoldWindow { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> HoldExpiryVerdict { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> AlertSubjectRole { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<int?> DeduplicationWindow { get; init; }
}
