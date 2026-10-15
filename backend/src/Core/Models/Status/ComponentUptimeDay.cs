using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Status;

public record ComponentUptimeDay : BaseModel
{
    public required ComponentUptimeDayId ComponentUptimeDayId { get; init; }
    public required ServiceComponent Component { get; init; }
    public required ObservationDate Date { get; init; }
    public required ProbeCount TotalProbes { get; init; }
    public required ProbeCount HealthyProbes { get; init; }

    public required DowntimeSeconds DowntimeSeconds { get; init; }

    public required UptimePercentage UptimePercentage { get; init; }
}
