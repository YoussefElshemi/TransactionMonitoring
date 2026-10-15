using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class ComponentProbeMapper
{
    public static ComponentProbe ToModel(this ComponentProbeEntity componentProbeEntity)
    {
        return new ComponentProbe
        {
            ComponentProbeId = new ComponentProbeId(componentProbeEntity.ComponentProbeId),
            Component = (ServiceComponent)componentProbeEntity.ComponentId,
            Source = (ProbeSource)componentProbeEntity.SourceId,
            ObservedAt = new ObservedAt(componentProbeEntity.ObservedAt),
            IsHealthy = new IsHealthy(componentProbeEntity.IsHealthy),
            LatencyMilliseconds = new LatencyMilliseconds(componentProbeEntity.LatencyMilliseconds),
            Detail = componentProbeEntity.Detail is null ? null : new ProbeDetail(componentProbeEntity.Detail),
            CreatedAt = new CreatedAt(componentProbeEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(componentProbeEntity.UpdatedAt)
        };
    }

    public static ComponentProbeEntity ToEntity(this ComponentProbe componentProbe)
    {
        return new ComponentProbeEntity
        {
            ComponentProbeId = componentProbe.ComponentProbeId,
            ComponentId = (int)componentProbe.Component,
            SourceId = (int)componentProbe.Source,
            ObservedAt = componentProbe.ObservedAt,
            IsHealthy = componentProbe.IsHealthy,
            LatencyMilliseconds = componentProbe.LatencyMilliseconds,
            Detail = componentProbe.Detail?.ToString(),
            CreatedAt = componentProbe.CreatedAt,
            UpdatedAt = componentProbe.UpdatedAt
        };
    }
}
