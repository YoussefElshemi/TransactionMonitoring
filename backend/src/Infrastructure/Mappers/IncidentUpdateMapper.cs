using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class IncidentUpdateMapper
{
    public static IncidentUpdate ToModel(this IncidentUpdateEntity incidentUpdateEntity)
    {
        return new IncidentUpdate
        {
            IncidentUpdateId = new IncidentUpdateId(incidentUpdateEntity.IncidentUpdateId),
            IncidentId = new IncidentId(incidentUpdateEntity.IncidentId),
            Status = (IncidentStatus)incidentUpdateEntity.StatusId,
            Body = new IncidentUpdateBody(incidentUpdateEntity.Body),
            PostedAt = new ObservedAt(incidentUpdateEntity.PostedAt),
            PostedByUserId = incidentUpdateEntity.PostedByUserId.HasValue ? new ApplicationUserId(incidentUpdateEntity.PostedByUserId.Value) : null,
            CreatedAt = new CreatedAt(incidentUpdateEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(incidentUpdateEntity.UpdatedAt)
        };
    }

    public static IncidentUpdateEntity ToEntity(this IncidentUpdate incidentUpdate)
    {
        return new IncidentUpdateEntity
        {
            IncidentUpdateId = incidentUpdate.IncidentUpdateId,
            IncidentId = incidentUpdate.IncidentId,
            StatusId = (int)incidentUpdate.Status,
            Body = incidentUpdate.Body,
            PostedAt = incidentUpdate.PostedAt,
            PostedByUserId = incidentUpdate.PostedByUserId.HasValue ? incidentUpdate.PostedByUserId.Value : null,
            CreatedAt = incidentUpdate.CreatedAt,
            UpdatedAt = incidentUpdate.UpdatedAt
        };
    }
}
