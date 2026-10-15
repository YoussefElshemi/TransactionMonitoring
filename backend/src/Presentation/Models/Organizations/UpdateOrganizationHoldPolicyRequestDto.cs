using System.Text.Json.Serialization;
using Core.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Organizations;

public record UpdateOrganizationHoldPolicyRequestDto
{
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required UpdateOrganizationHoldPolicyRequestBodyDto Body { get; init; }
}

public record UpdateOrganizationHoldPolicyRequestBodyDto
{
    // Omitted when unset so a round-tripped body doesn't turn "leave alone" into "clear".
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<int?> HoldWindow { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> HoldExpiryVerdict { get; init; }
}
