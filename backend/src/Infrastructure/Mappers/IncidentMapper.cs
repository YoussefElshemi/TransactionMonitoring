using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class IncidentMapper
{
    public static Incident ToModel(this IncidentEntity incidentEntity)
    {
        return new Incident
        {
            IncidentId = new IncidentId(incidentEntity.IncidentId),
            IncidentNumber = new IncidentNumber(incidentEntity.IncidentNumber),
            Title = new IncidentTitle(incidentEntity.Title),
            Summary = new IncidentSummary(incidentEntity.Summary),
            Status = (IncidentStatus)incidentEntity.StatusId,
            Impact = (IncidentImpact)incidentEntity.ImpactId,
            Components = incidentEntity.Components
                .Select(x => (ServiceComponent)x.ComponentId)
                .OrderBy(x => x)
                .ToList(),
            StartedAt = new StartedAt(incidentEntity.StartedAt),
            ResolvedAt = incidentEntity.ResolvedAt.HasValue ? new ResolvedAt(incidentEntity.ResolvedAt.Value) : null,
            RootCause = incidentEntity.RootCause is null ? null : new RootCause(incidentEntity.RootCause),
            Remediation = incidentEntity.Remediation is null ? null : new Remediation(incidentEntity.Remediation),
            ReviewPublishedAt = incidentEntity.ReviewPublishedAt.HasValue ? new PublishedAt(incidentEntity.ReviewPublishedAt.Value) : null,
            CreatedByUserId = incidentEntity.CreatedByUserId.HasValue ? new ApplicationUserId(incidentEntity.CreatedByUserId.Value) : null,
            Updates = incidentEntity.Updates
                .OrderBy(x => x.PostedAt)
                .Select(x => x.ToModel())
                .ToList(),
            CreatedAt = new CreatedAt(incidentEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(incidentEntity.UpdatedAt),
            IsDeleted = new IsDeleted(incidentEntity.IsDeleted),
            DeletedAt = incidentEntity.DeletedAt.HasValue ? new DeletedAt(incidentEntity.DeletedAt.Value) : null
        };
    }

    public static IncidentEntity ToEntity(this Incident incident)
    {
        return new IncidentEntity
        {
            IncidentId = incident.IncidentId,
            IncidentNumber = incident.IncidentNumber,
            Title = incident.Title,
            Summary = incident.Summary,
            StatusId = (int)incident.Status,
            ImpactId = (int)incident.Impact,
            StartedAt = incident.StartedAt,
            ResolvedAt = incident.ResolvedAt.HasValue ? incident.ResolvedAt.Value : null,
            RootCause = incident.RootCause?.ToString(),
            Remediation = incident.Remediation?.ToString(),
            ReviewPublishedAt = incident.ReviewPublishedAt.HasValue ? incident.ReviewPublishedAt.Value : null,
            CreatedByUserId = incident.CreatedByUserId.HasValue ? incident.CreatedByUserId.Value : null,
            Components = incident.Components
                .Select(x => new IncidentComponentEntity
                {
                    IncidentId = incident.IncidentId,
                    ComponentId = (int)x,
                    CreatedAt = incident.CreatedAt,
                    UpdatedAt = incident.UpdatedAt
                })
                .ToList(),
            CreatedAt = incident.CreatedAt,
            UpdatedAt = incident.UpdatedAt,
            IsDeleted = incident.IsDeleted,
            DeletedAt = incident.DeletedAt
        };
    }
}
