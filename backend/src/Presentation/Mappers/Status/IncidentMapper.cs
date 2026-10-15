using Core.Models.Status;
using Core.ValueObjects;
using Presentation.Models.Status;

namespace Presentation.Mappers.Status;

public static class IncidentMapper
{
    public static CreateIncidentRequest ToModel(this CreateIncidentRequestDto dto)
    {
        return new CreateIncidentRequest
        {
            Title = new IncidentTitle(dto.Body.Title),
            Summary = new IncidentSummary(dto.Body.Summary),
            Impact = dto.Body.Impact,
            Components = dto.Body.Components,
            StartedAt = dto.Body.StartedAt.HasValue ? new StartedAt(dto.Body.StartedAt.Value) : null,
            CreatedByUserId = dto.CreatedByUserId.HasValue ? new ApplicationUserId(dto.CreatedByUserId.Value) : null
        };
    }

    public static UpdateIncidentRequest ToModel(this UpdateIncidentRequestDto dto)
    {
        return new UpdateIncidentRequest
        {
            IncidentId = new IncidentId(dto.IncidentId),
            Title = new IncidentTitle(dto.Body.Title),
            Summary = new IncidentSummary(dto.Body.Summary),
            Impact = dto.Body.Impact,
            Components = dto.Body.Components,
            StartedAt = new StartedAt(dto.Body.StartedAt)
        };
    }

    public static PostIncidentUpdateRequest ToModel(this PostIncidentUpdateRequestDto dto)
    {
        return new PostIncidentUpdateRequest
        {
            IncidentId = new IncidentId(dto.IncidentId),
            Status = dto.Body.Status,
            Body = new IncidentUpdateBody(dto.Body.Body),
            PostedByUserId = dto.PostedByUserId.HasValue ? new ApplicationUserId(dto.PostedByUserId.Value) : null
        };
    }

    public static PublishIncidentReviewRequest ToModel(this PublishIncidentReviewRequestDto dto)
    {
        return new PublishIncidentReviewRequest
        {
            IncidentId = new IncidentId(dto.IncidentId),
            RootCause = new RootCause(dto.Body.RootCause),
            Remediation = new Remediation(dto.Body.Remediation)
        };
    }

    public static ListIncidentRequest ToModel(this ListIncidentRequestDto dto)
    {
        return new ListIncidentRequest
        {
            Component = dto.Component,
            Status = dto.Status,
            IncludeResolved = new IncludeResolved(dto.IncludeResolved ?? false),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }

    public static IncidentResponseDto ToDto(this Incident incident)
    {
        return new IncidentResponseDto
        {
            IncidentId = incident.IncidentId,
            IncidentNumber = incident.IncidentNumber,
            Title = incident.Title,
            Summary = incident.Summary,
            Status = incident.Status.ToString(),
            Impact = incident.Impact.ToString(),
            Components = incident.Components.ConvertAll(x => x.ToString()),
            StartedAt = incident.StartedAt,
            ResolvedAt = incident.ResolvedAt.HasValue ? incident.ResolvedAt.Value : null,
            RootCause = incident.RootCause?.ToString(),
            Remediation = incident.Remediation?.ToString(),
            ReviewPublishedAt = incident.ReviewPublishedAt.HasValue ? incident.ReviewPublishedAt.Value : null,
            Updates = incident.Updates.ConvertAll(x => x.ToDto()),
            CreatedAt = incident.CreatedAt,
            UpdatedAt = incident.UpdatedAt
        };
    }

    public static IncidentUpdateResponseDto ToDto(this IncidentUpdate incidentUpdate)
    {
        return new IncidentUpdateResponseDto
        {
            IncidentUpdateId = incidentUpdate.IncidentUpdateId,
            Status = incidentUpdate.Status.ToString(),
            Body = incidentUpdate.Body,
            PostedAt = incidentUpdate.PostedAt
        };
    }
}
