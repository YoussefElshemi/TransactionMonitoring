using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Mappers;

public static class PostIncidentUpdateRequestMapper
{
    public static IncidentUpdate ToModel(this PostIncidentUpdateRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new IncidentUpdate
        {
            IncidentUpdateId = new IncidentUpdateId(Guid.NewGuid()),
            IncidentId = request.IncidentId,
            Status = request.Status,
            Body = request.Body,
            PostedAt = new ObservedAt(now),
            PostedByUserId = request.PostedByUserId,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }
}
