namespace Core.Configs;

public record StatusConfig
{
    public int UnhealthyProbeThreshold { get; init; } = 3;

    public int DegradedLatencyMilliseconds { get; init; } = 2_000;

    public int ProbeRetentionDays { get; init; } = 30;

    public bool RaiseIncidentsFromProbes { get; init; } = true;

    public int MinimumMaintenanceNoticeHours { get; init; } = 24;

    public int HeartbeatToleranceIntervals { get; init; } = 3;

    public int AvailabilityFlushSeconds { get; init; } = 30;
}
