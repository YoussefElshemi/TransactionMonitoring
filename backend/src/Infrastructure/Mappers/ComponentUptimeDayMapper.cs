using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class ComponentUptimeDayMapper
{
    public static ComponentUptimeDay ToModel(this ComponentUptimeDayEntity componentUptimeDayEntity)
    {
        return new ComponentUptimeDay
        {
            ComponentUptimeDayId = new ComponentUptimeDayId(componentUptimeDayEntity.ComponentUptimeDayId),
            Component = (ServiceComponent)componentUptimeDayEntity.ComponentId,
            Date = new ObservationDate(componentUptimeDayEntity.Date),
            TotalProbes = new ProbeCount(componentUptimeDayEntity.TotalProbes),
            HealthyProbes = new ProbeCount(componentUptimeDayEntity.HealthyProbes),
            DowntimeSeconds = new DowntimeSeconds(componentUptimeDayEntity.DowntimeSeconds),
            UptimePercentage = new UptimePercentage(componentUptimeDayEntity.UptimePercentage),
            CreatedAt = new CreatedAt(componentUptimeDayEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(componentUptimeDayEntity.UpdatedAt)
        };
    }

    public static ComponentUptimeDayEntity ToEntity(this ComponentUptimeDay componentUptimeDay)
    {
        return new ComponentUptimeDayEntity
        {
            ComponentUptimeDayId = componentUptimeDay.ComponentUptimeDayId,
            ComponentId = (int)componentUptimeDay.Component,
            Date = componentUptimeDay.Date,
            TotalProbes = componentUptimeDay.TotalProbes,
            HealthyProbes = componentUptimeDay.HealthyProbes,
            DowntimeSeconds = componentUptimeDay.DowntimeSeconds,
            UptimePercentage = componentUptimeDay.UptimePercentage,
            CreatedAt = componentUptimeDay.CreatedAt,
            UpdatedAt = componentUptimeDay.UpdatedAt
        };
    }
}
