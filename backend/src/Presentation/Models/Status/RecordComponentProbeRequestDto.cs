using Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Status;

public record RecordComponentProbeRequestDto
{
    [FromBody]
    public required RecordComponentProbeRequestBodyDto Body { get; init; }
}

public record RecordComponentProbeRequestBodyDto
{
    public required ServiceComponent Component { get; init; }
    public required bool IsHealthy { get; init; }
    public required int LatencyMilliseconds { get; init; }

    public DateTimeOffset? ObservedAt { get; init; }

    public string? Detail { get; init; }
}
