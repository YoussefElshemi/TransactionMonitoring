using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.VelocityProfiles;

public record SetVelocityProfileRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required SetVelocityProfileRequestBodyDto Body { get; init; }
}

public record SetVelocityProfileRequestBodyDto
{
    public required List<VelocityMetricDto> Metrics { get; init; }

    public List<VelocityThresholdDto>? Thresholds { get; init; }
}
