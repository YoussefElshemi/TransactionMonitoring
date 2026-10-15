using Core.Models.Availability;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IAvailabilityCommitmentRepository : IPagedRepository<ListAvailabilityCommitmentRequest, AvailabilityCommitment>
{
    Task AddAsync(AvailabilityCommitment availabilityCommitment, CancellationToken stoppingToken);
    Task<AvailabilityCommitment> GetAsync(AvailabilityCommitmentId availabilityCommitmentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, AvailabilityCommitmentId availabilityCommitmentId, CancellationToken stoppingToken);

    Task<AvailabilityCommitment?> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken);

    Task<List<AvailabilityCommitment>> ListInEffectAsync(DateTimeOffset asOf, CancellationToken stoppingToken);

    Task CloseAsync(AvailabilityCommitmentId availabilityCommitmentId, EffectiveTo effectiveTo, CancellationToken stoppingToken);
    Task DeleteAsync(AvailabilityCommitmentId availabilityCommitmentId, CancellationToken stoppingToken);
}
