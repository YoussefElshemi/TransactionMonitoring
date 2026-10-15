using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateIncidentRequestMapper
{
    public static Incident ToModel(this CreateIncidentRequest request, IncidentNumber incidentNumber, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new Incident
        {
            IncidentId = new IncidentId(Guid.NewGuid()),
            IncidentNumber = incidentNumber,
            Title = request.Title,
            Summary = request.Summary,

            Status = IncidentStatus.Investigating,

            Impact = request.Impact,
            Components = request.Components.Distinct().OrderBy(x => x).ToList(),
            StartedAt = request.StartedAt ?? new StartedAt(now),
            ResolvedAt = null,
            RootCause = null,
            Remediation = null,
            ReviewPublishedAt = null,
            CreatedByUserId = request.CreatedByUserId,
            Updates = [],
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
