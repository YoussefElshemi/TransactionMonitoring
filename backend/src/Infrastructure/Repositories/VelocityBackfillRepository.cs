using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Common;
using Core.Models.Transactions;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VelocityBackfillRepository(
    TransactionMonitoringDbContext context,
    IPayloadProtectionService payloadProtectionService,
    TimeProvider timeProvider) : PagedRepository<ListVelocityBackfillRequest, VelocityBackfillEntity, VelocityBackfill>, IVelocityBackfillRepository
{
    public async Task AddAsync(VelocityBackfill velocityBackfill, CancellationToken stoppingToken)
    {
        await context.VelocityBackfills.AddAsync(velocityBackfill.ToEntity(), stoppingToken);
    }

    public async Task<VelocityBackfill> GetAsync(VelocityBackfillId velocityBackfillId, CancellationToken stoppingToken)
    {
        var velocityBackfillEntity = await context.VelocityBackfills
            .FirstAsync(x => x.VelocityBackfillId == velocityBackfillId, stoppingToken);

        return velocityBackfillEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, VelocityBackfillId velocityBackfillId, CancellationToken stoppingToken)
    {
        var exists = await context.VelocityBackfills
            .AnyAsync(x => x.VelocityBackfillId == velocityBackfillId && x.OrganizationId == organizationId, stoppingToken);

        return new Exists(exists);
    }

    public async Task UpdateAsync(
        VelocityBackfillId velocityBackfillId,
        VelocityBackfillStatus status,
        TotalCount totalTransactions,
        ProcessedCount processedCount,
        CompletedAt? completedAt,
        CancellationToken stoppingToken)
    {
        var velocityBackfillEntity = await context.VelocityBackfills.FindAsync([(Guid)velocityBackfillId], stoppingToken);

        velocityBackfillEntity!.StatusId = (int)status;
        velocityBackfillEntity.TotalTransactions = totalTransactions;
        velocityBackfillEntity.ProcessedCount = processedCount;
        velocityBackfillEntity.CompletedAt = completedAt;
        velocityBackfillEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.VelocityBackfills.Update(velocityBackfillEntity);
    }

    public async Task<TotalCount> CountTransactionsAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var count = await Backfillable(organizationId).CountAsync(stoppingToken);

        return new TotalCount(count);
    }

    public async Task<List<Transaction>> ListTransactionChunkAsync(OrganizationId organizationId, ProcessedCount skip, int chunkSize, CancellationToken stoppingToken)
    {
        var transactionEntities = await Backfillable(organizationId)
            .Include(x => x.TransactionParties)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.TransactionId)
            .Skip(skip)
            .Take(chunkSize)
            .ToListAsync(stoppingToken);

        var dataKey = await payloadProtectionService.FindDataKeyAsync(organizationId, stoppingToken);

        return transactionEntities.ConvertAll(x => x.ToModel(dataKey));
    }

    protected override Task<List<VelocityBackfill>> MapPageAsync(List<VelocityBackfillEntity> items, ListVelocityBackfillRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<VelocityBackfillEntity> OrderByExpression()
    {
        return new OrderByExpression<VelocityBackfillEntity, DateTimeOffset>(x => x.CreatedAt);
    }

    protected override IQueryable<VelocityBackfillEntity> FilterByPagedRequest(ListVelocityBackfillRequest request)
    {
        return context.VelocityBackfills.Where(x => x.OrganizationId == request.OrganizationId);
    }

    protected override IQueryable<VelocityBackfillEntity> FilterByDateRange(IQueryable<VelocityBackfillEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }

    private IQueryable<TransactionEntity> Backfillable(OrganizationId organizationId)
    {
        return context.Transactions
            .IgnoreAutoIncludes()
            .Where(x => x.OrganizationId == organizationId && !x.IsDeleted && x.TransactionParties.Any());
    }
}
