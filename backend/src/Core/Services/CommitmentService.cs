using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Commitments;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Services;

public class CommitmentService(
    ICommitmentRepository commitmentRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommitmentService
{
    public async Task<Commitment> CreateAsync(CreateCommitmentRequest createCommitmentRequest, CancellationToken stoppingToken)
    {
        var commitment = createCommitmentRequest.ToModel(timeProvider);

        if (commitment.EffectiveTo <= (DateTimeOffset)commitment.EffectiveFrom)
        {
            throw new InvalidPriceStructureException("a commitment cannot end before it starts.");
        }

        if (await commitmentRepository.OverlapsAsync(commitment.OrganizationId, commitment.EffectiveFrom, commitment.EffectiveTo, stoppingToken))
        {
            throw new CommitmentOverlapException(commitment.OrganizationId);
        }

        await commitmentRepository.AddAsync(commitment, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(commitment.CommitmentId, stoppingToken);
    }

    public Task<Commitment> GetAsync(CommitmentId commitmentId, CancellationToken stoppingToken)
    {
        return commitmentRepository.GetAsync(commitmentId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, CommitmentId commitmentId, CancellationToken stoppingToken)
    {
        return commitmentRepository.ExistsAsync(organizationId, commitmentId, stoppingToken);
    }

    public Task<Commitment?> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        return commitmentRepository.GetInEffectAsync(organizationId, asOf, stoppingToken);
    }

    public Task<PagedResponse<Commitment>> ListAsync(ListCommitmentRequest listCommitmentRequest, CancellationToken stoppingToken)
    {
        return commitmentRepository.ListAsync(listCommitmentRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListCommitmentRequest listCommitmentRequest, CancellationToken stoppingToken)
    {
        return commitmentRepository.GetCountAsync(listCommitmentRequest, stoppingToken);
    }

    public async Task DeleteAsync(CommitmentId commitmentId, CancellationToken stoppingToken)
    {
        var commitment = await GetAsync(commitmentId, stoppingToken);

        if (commitment.Drawn > 0)
        {
            throw new CommitmentInUseException(commitment.CommitmentId);
        }

        await commitmentRepository.DeleteAsync(commitmentId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }
}
