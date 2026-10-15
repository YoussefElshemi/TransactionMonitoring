using System.Text.Json.Serialization;
using Presentation.Models.Organizations;

namespace Presentation.Models.CaseReasonCodes;

public record CaseReasonCodeResourceDto
{
    public required CaseReasonCodeResponseDto CaseReasonCode { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}
