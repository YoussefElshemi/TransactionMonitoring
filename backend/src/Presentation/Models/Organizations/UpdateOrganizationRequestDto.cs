using System.Text.Json.Serialization;
using Core.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Organizations;

public record UpdateOrganizationRequestDto : OrganizationRequestDto
{
    [FromBody]
    public required UpdateOrganizationRequestBodyDto Body { get; init; }
}

public record UpdateOrganizationRequestBodyDto
{
    public string? FriendlyName { get; init; }
    public string? Status { get; init; }
    // Omitted when unset so a round-tripped body doesn't turn "leave alone" into "clear".
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<int?> HoldWindow { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> HoldExpiryVerdict { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<int?> SandboxMonthlyCredits { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> DefaultLocale { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> DefaultTimeZone { get; init; }
}