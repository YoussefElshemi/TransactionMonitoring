using Core.Models.Commitments;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ICommitmentRepository : IPagedRepository<ListCommitmentRequest, Commitment>
{
    Task AddAsync(Commitment commitment, CancellationToken stoppingToken);
    Task<Commitment> GetAsync(CommitmentId commitmentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CommitmentId commitmentId, CancellationToken stoppingToken);

    Task<Commitment?> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken);

    Task<Exists> OverlapsAsync(OrganizationId organizationId, EffectiveFrom effectiveFrom, EffectiveTo effectiveTo, CancellationToken stoppingToken);
    Task DeleteAsync(CommitmentId commitmentId, CancellationToken stoppingToken);
}
