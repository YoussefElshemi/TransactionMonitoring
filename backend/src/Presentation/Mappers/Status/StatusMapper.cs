using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;
using Presentation.Models.Status;

namespace Presentation.Mappers.Status;

public static class StatusMapper
{
    public static ServiceStatusResponseDto ToDto(this ServiceStatus serviceStatus)
    {
        return new ServiceStatusResponseDto
        {
            Status = serviceStatus.Status.ToString(),
            ObservedAt = serviceStatus.ObservedAt,
            Components = serviceStatus.Components.ConvertAll(x => x.ToDto()),
            ActiveIncidents = serviceStatus.ActiveIncidents.ConvertAll(x => x.ToDto()),
            UpcomingMaintenance = serviceStatus.UpcomingMaintenance.ConvertAll(x => x.ToDto())
        };
    }

    public static ComponentHealthResponseDto ToDto(this ComponentHealth componentHealth)
    {
        return new ComponentHealthResponseDto
        {
            Component = componentHealth.Component.ToString(),
            Status = componentHealth.Status.ToString(),
            LastProbedAt = componentHealth.LastProbedAt.HasValue ? componentHealth.LastProbedAt.Value : null,
            LatencyMilliseconds = componentHealth.LatencyMilliseconds.HasValue ? componentHealth.LatencyMilliseconds.Value : null,
            UptimePercentage = componentHealth.UptimePercentage,
            History = componentHealth.History.ConvertAll(x => x.ToDto())
        };
    }

    public static RecordComponentProbeRequest ToModel(this RecordComponentProbeRequestDto dto)
    {
        return new RecordComponentProbeRequest
        {
            Component = dto.Body.Component,

            Source = ProbeSource.External,

            ObservedAt = dto.Body.ObservedAt.HasValue ? new ObservedAt(dto.Body.ObservedAt.Value) : null,
            IsHealthy = new IsHealthy(dto.Body.IsHealthy),
            LatencyMilliseconds = new LatencyMilliseconds(dto.Body.LatencyMilliseconds),
            Detail = string.IsNullOrWhiteSpace(dto.Body.Detail) ? null : new ProbeDetail(dto.Body.Detail)
        };
    }

    public static ComponentProbeResponseDto ToDto(this ComponentProbe componentProbe)
    {
        return new ComponentProbeResponseDto
        {
            ComponentProbeId = componentProbe.ComponentProbeId,
            Component = componentProbe.Component.ToString(),
            Source = componentProbe.Source.ToString(),
            ObservedAt = componentProbe.ObservedAt,
            IsHealthy = componentProbe.IsHealthy,
            LatencyMilliseconds = componentProbe.LatencyMilliseconds,
            Detail = componentProbe.Detail?.ToString()
        };
    }

    public static ServiceHeartbeatResponseDto ToDto(this ServiceHeartbeat serviceHeartbeat)
    {
        return new ServiceHeartbeatResponseDto
        {
            ObservedAt = serviceHeartbeat.ObservedAt,
            ExpectedIntervalSeconds = serviceHeartbeat.ExpectedIntervalSeconds,
            IsFresh = serviceHeartbeat.IsFresh,
            Components = serviceHeartbeat.Components.ConvertAll(x => new ComponentHeartbeatResponseDto
            {
                Component = x.Component.ToString(),
                LastProbedAt = x.LastProbedAt.HasValue ? x.LastProbedAt.Value : null,
                StaleForSeconds = x.StaleForSeconds.HasValue ? x.StaleForSeconds.Value : null,
                IsFresh = x.IsFresh
            })
        };
    }

    public static ComponentUptimeDayResponseDto ToDto(this ComponentUptimeDay componentUptimeDay)
    {
        return new ComponentUptimeDayResponseDto
        {
            Date = componentUptimeDay.Date,
            TotalProbes = componentUptimeDay.TotalProbes,
            HealthyProbes = componentUptimeDay.HealthyProbes,
            DowntimeSeconds = componentUptimeDay.DowntimeSeconds,
            UptimePercentage = componentUptimeDay.UptimePercentage
        };
    }
}
