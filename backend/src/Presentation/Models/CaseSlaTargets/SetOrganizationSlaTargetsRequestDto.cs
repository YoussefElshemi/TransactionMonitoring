using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.CaseSlaTargets;

public record SetOrganizationSlaTargetsRequestDto : OrganizationSlaTargetRequestDto
{
    [FromBody]
    public required SetOrganizationSlaTargetsRequestBodyDto Body { get; init; }
}

public record SetOrganizationSlaTargetsRequestBodyDto
{
    public required SlaTargetRequestDto[] Targets { get; init; }
}

public record SlaTargetRequestDto
{
    public required string Severity { get; init; }
    public required int TargetMinutes { get; init; }
}
