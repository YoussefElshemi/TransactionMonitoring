using Core.Models.Availability;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IAvailabilityCommitmentService : IPagedService<ListAvailabilityCommitmentRequest, AvailabilityCommitment>
{
    Task<AvailabilityCommitment> CreateAsync(CreateAvailabilityCommitmentRequest createAvailabilityCommitmentRequest, CancellationToken stoppingToken);
    Task<AvailabilityCommitment> GetAsync(AvailabilityCommitmentId availabilityCommitmentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, AvailabilityCommitmentId availabilityCommitmentId, CancellationToken stoppingToken);
    Task<AvailabilityCommitment?> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken);
    Task<List<AvailabilityCommitment>> ListInEffectAsync(DateTimeOffset asOf, CancellationToken stoppingToken);
    Task DeleteAsync(AvailabilityCommitmentId availabilityCommitmentId, CancellationToken stoppingToken);
}
