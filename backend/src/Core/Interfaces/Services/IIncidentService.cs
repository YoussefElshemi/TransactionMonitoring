using Core.Enums;
using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IIncidentService : IPagedService<ListIncidentRequest, Incident>
{
    Task<Incident> CreateAsync(CreateIncidentRequest createIncidentRequest, CancellationToken stoppingToken);
    Task<Incident> GetAsync(IncidentId incidentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(IncidentId incidentId, CancellationToken stoppingToken);
    Task<Incident> UpdateAsync(UpdateIncidentRequest updateIncidentRequest, CancellationToken stoppingToken);

    Task<Incident> PostUpdateAsync(PostIncidentUpdateRequest postIncidentUpdateRequest, CancellationToken stoppingToken);

    Task<Incident> PublishReviewAsync(PublishIncidentReviewRequest publishIncidentReviewRequest, CancellationToken stoppingToken);

    Task<List<Incident>> ListActiveAsync(CancellationToken stoppingToken);
    Task<List<Incident>> ListActiveForComponentAsync(ServiceComponent component, CancellationToken stoppingToken);
}
