using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Services;

public class IncidentService(
    IIncidentRepository incidentRepository,
    IStatusNotifier statusNotifier,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IIncidentService
{
    public async Task<Incident> CreateAsync(CreateIncidentRequest createIncidentRequest, CancellationToken stoppingToken)
    {
        var incidentNumber = await incidentRepository.GenerateIncidentNumberAsync(stoppingToken);
        var incident = createIncidentRequest.ToModel(incidentNumber, timeProvider);

        await incidentRepository.AddAsync(incident, stoppingToken);

        await incidentRepository.AddUpdateAsync(new PostIncidentUpdateRequest
        {
            IncidentId = incident.IncidentId,
            Status = IncidentStatus.Investigating,
            Body = new IncidentUpdateBody(createIncidentRequest.Summary),
            PostedByUserId = createIncidentRequest.CreatedByUserId
        }.ToModel(timeProvider), stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        var created = await GetAsync(incident.IncidentId, stoppingToken);

        await statusNotifier.NotifySubscribersAsync(created, created.Updates[^1], stoppingToken);

        return created;
    }

    public Task<Incident> GetAsync(IncidentId incidentId, CancellationToken stoppingToken)
    {
        return incidentRepository.GetAsync(incidentId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(IncidentId incidentId, CancellationToken stoppingToken)
    {
        return incidentRepository.ExistsAsync(incidentId, stoppingToken);
    }

    public async Task<Incident> UpdateAsync(UpdateIncidentRequest updateIncidentRequest, CancellationToken stoppingToken)
    {
        var incident = await GetAsync(updateIncidentRequest.IncidentId, stoppingToken);

        var amended = incident with
        {
            Title = updateIncidentRequest.Title,
            Summary = updateIncidentRequest.Summary,
            Impact = updateIncidentRequest.Impact,
            Components = updateIncidentRequest.Components.Distinct().OrderBy(x => x).ToList(),
            StartedAt = updateIncidentRequest.StartedAt
        };

        await incidentRepository.UpdateAsync(amended, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(incident.IncidentId, stoppingToken);
    }

    public async Task<Incident> PostUpdateAsync(PostIncidentUpdateRequest postIncidentUpdateRequest, CancellationToken stoppingToken)
    {
        var incident = await GetAsync(postIncidentUpdateRequest.IncidentId, stoppingToken);

        if (incident.Status == IncidentStatus.Resolved)
        {
            throw new IncidentResolvedException(incident.IncidentId);
        }

        var incidentUpdate = postIncidentUpdateRequest.ToModel(timeProvider);

        var amended = incident with
        {
            Status = postIncidentUpdateRequest.Status,
            ResolvedAt = postIncidentUpdateRequest.Status == IncidentStatus.Resolved
                ? new ResolvedAt(incidentUpdate.PostedAt)
                : incident.ResolvedAt
        };

        await incidentRepository.AddUpdateAsync(incidentUpdate, stoppingToken);
        await incidentRepository.UpdateAsync(amended, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        var updated = await GetAsync(incident.IncidentId, stoppingToken);

        await statusNotifier.NotifySubscribersAsync(updated, incidentUpdate, stoppingToken);

        return updated;
    }

    public async Task<Incident> PublishReviewAsync(PublishIncidentReviewRequest publishIncidentReviewRequest, CancellationToken stoppingToken)
    {
        var incident = await GetAsync(publishIncidentReviewRequest.IncidentId, stoppingToken);

        if (incident.Status != IncidentStatus.Resolved)
        {
            throw new IncidentNotResolvedException(incident.IncidentId);
        }

        var amended = incident with
        {
            RootCause = publishIncidentReviewRequest.RootCause,
            Remediation = publishIncidentReviewRequest.Remediation,

            ReviewPublishedAt = incident.ReviewPublishedAt ?? new PublishedAt(timeProvider.GetUtcNow())
        };

        await incidentRepository.UpdateAsync(amended, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(incident.IncidentId, stoppingToken);
    }

    public Task<List<Incident>> ListActiveAsync(CancellationToken stoppingToken)
    {
        return incidentRepository.ListActiveAsync(stoppingToken);
    }

    public Task<List<Incident>> ListActiveForComponentAsync(ServiceComponent component, CancellationToken stoppingToken)
    {
        return incidentRepository.ListActiveForComponentAsync(component, stoppingToken);
    }

    public Task<PagedResponse<Incident>> ListAsync(ListIncidentRequest listIncidentRequest, CancellationToken stoppingToken)
    {
        return incidentRepository.ListAsync(listIncidentRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListIncidentRequest listIncidentRequest, CancellationToken stoppingToken)
    {
        return incidentRepository.GetCountAsync(listIncidentRequest, stoppingToken);
    }
}
