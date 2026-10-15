using Core.Interfaces.Repositories;
using Core.Models.Commitments;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CommitmentRepository(
    TransactionMonitoringDbContext context,
    IInvoiceRepository invoiceRepository,
    TimeProvider timeProvider) : PagedRepository<ListCommitmentRequest, CommitmentEntity, Commitment>, ICommitmentRepository
{
    public async Task AddAsync(Commitment commitment, CancellationToken stoppingToken)
    {
        var commitmentEntity = commitment.ToEntity();
        await context.Commitments.AddAsync(commitmentEntity, stoppingToken);
    }

    public async Task<Commitment> GetAsync(CommitmentId commitmentId, CancellationToken stoppingToken)
    {
        var commitmentEntity = await context.Commitments
            .FirstAsync(x => x.CommitmentId == commitmentId && !x.IsDeleted, stoppingToken);

        return await WithDrawnAsync(commitmentEntity, stoppingToken);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, CommitmentId commitmentId, CancellationToken stoppingToken)
    {
        var exists = await context.Commitments
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.CommitmentId == commitmentId &&
                           !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Commitment?> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        var commitmentEntity = await context.Commitments
            .Where(x => x.OrganizationId == organizationId &&
                        !x.IsDeleted &&
                        x.EffectiveFrom <= asOf &&
                        x.EffectiveTo > asOf)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(stoppingToken);

        return commitmentEntity is null ? null : await WithDrawnAsync(commitmentEntity, stoppingToken);
    }

    public async Task<Exists> OverlapsAsync(OrganizationId organizationId, EffectiveFrom effectiveFrom, EffectiveTo effectiveTo, CancellationToken stoppingToken)
    {
        var from = (DateTimeOffset)effectiveFrom;
        var to = (DateTimeOffset)effectiveTo;

        var exists = await context.Commitments
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           !x.IsDeleted &&
                           x.EffectiveFrom < to &&
                           x.EffectiveTo > from, stoppingToken);

        return new Exists(exists);
    }

    public async Task DeleteAsync(CommitmentId commitmentId, CancellationToken stoppingToken)
    {
        var commitmentEntity = await context.Commitments
            .FirstAsync(x => x.CommitmentId == commitmentId && !x.IsDeleted, stoppingToken);

        commitmentEntity.IsDeleted = true;
        commitmentEntity.DeletedAt = timeProvider.GetUtcNow();
        commitmentEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Commitments.Update(commitmentEntity);
    }

    private async Task<Commitment> WithDrawnAsync(CommitmentEntity commitmentEntity, CancellationToken stoppingToken)
    {
        var drawn = await invoiceRepository.GetCommitmentDrawnAsync(
            new OrganizationId(commitmentEntity.OrganizationId),
            new EffectiveFrom(commitmentEntity.EffectiveFrom),
            new EffectiveTo(commitmentEntity.EffectiveTo),
            stoppingToken);

        return commitmentEntity.ToModel(drawn);
    }

    protected override async Task<List<Commitment>> MapPageAsync(List<CommitmentEntity> items, ListCommitmentRequest request, CancellationToken stoppingToken)
    {
        var commitments = new List<Commitment>(items.Count);

        foreach (var item in items)
        {
            commitments.Add(await WithDrawnAsync(item, stoppingToken));
        }

        return commitments;
    }

    protected override IOrderByExpression<CommitmentEntity> OrderByExpression()
    {
        return new OrderByExpression<CommitmentEntity, DateTimeOffset>(x => x.EffectiveFrom);
    }

    protected override IQueryable<CommitmentEntity> FilterByPagedRequest(ListCommitmentRequest listCommitmentRequest)
    {
        var queryable = context.Commitments
            .Where(x => x.OrganizationId == listCommitmentRequest.OrganizationId && !x.IsDeleted);

        if (!listCommitmentRequest.IncludeExpired)
        {
            var now = timeProvider.GetUtcNow();
            queryable = queryable.Where(x => x.EffectiveTo > now);
        }

        return queryable;
    }

    protected override IQueryable<CommitmentEntity> FilterByDateRange(IQueryable<CommitmentEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }
}
