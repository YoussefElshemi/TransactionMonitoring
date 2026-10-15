using Core.Models.Commitments;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface ICommitmentService : IPagedService<ListCommitmentRequest, Commitment>
{
    Task<Commitment> CreateAsync(CreateCommitmentRequest createCommitmentRequest, CancellationToken stoppingToken);
    Task<Commitment> GetAsync(CommitmentId commitmentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CommitmentId commitmentId, CancellationToken stoppingToken);
    Task<Commitment?> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken);
    Task DeleteAsync(CommitmentId commitmentId, CancellationToken stoppingToken);
}
