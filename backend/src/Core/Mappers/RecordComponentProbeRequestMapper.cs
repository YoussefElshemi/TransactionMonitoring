using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Mappers;

public static class RecordComponentProbeRequestMapper
{
    public static ComponentProbe ToModel(this RecordComponentProbeRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new ComponentProbe
        {
            ComponentProbeId = new ComponentProbeId(Guid.NewGuid()),
            Component = request.Component,
            Source = request.Source,

            ObservedAt = request.ObservedAt ?? new ObservedAt(now),
            IsHealthy = request.IsHealthy,
            LatencyMilliseconds = request.LatencyMilliseconds,
            Detail = request.Detail,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }
}
