using System.Text.Json.Serialization;
using Core.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Organizations;

public record UpdateOrganizationAlertPolicyRequestDto
{
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required UpdateOrganizationAlertPolicyRequestBodyDto Body { get; init; }
}

public record UpdateOrganizationAlertPolicyRequestBodyDto
{
    // Omitted when unset so a round-tripped body doesn't turn "leave alone" into "clear".
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<bool?> CaseGroupingEnabled { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<bool?> DeduplicationEnabled { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<int?> CaseGroupingPeriod { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<int?> DeduplicationWindow { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> CaseGroupingScope { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> CaseGroupingSeverityPolicy { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<int?> MaxAlertsPerCase { get; init; }
}
