using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TransactionSchemaVersionRepository(
    TransactionMonitoringDbContext context) : PagedRepository<ListTransactionSchemaVersionRequest, TransactionSchemaVersionEntity, TransactionSchemaVersion>, ITransactionSchemaVersionRepository
{
    public async Task AddAsync(TransactionSchemaVersion transactionSchemaVersion, CancellationToken stoppingToken)
    {
        var transactionSchemaVersionEntity = transactionSchemaVersion.ToEntity();
        await context.TransactionSchemaVersions.AddAsync(transactionSchemaVersionEntity, stoppingToken);
    }

    public async Task<TransactionSchemaVersion> GetByVersionNumberAsync(TransactionSchemaId transactionSchemaId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        var transactionSchemaVersionEntity = await context.TransactionSchemaVersions
            .FirstAsync(x => x.TransactionSchemaId == transactionSchemaId && x.VersionNumber == versionNumber, stoppingToken);

        return transactionSchemaVersionEntity.ToModel();
    }

    public async Task<VersionNumber> GetMaxVersionNumberAsync(TransactionSchemaId transactionSchemaId, CancellationToken stoppingToken)
    {
        var maxVersionNumber = await context.TransactionSchemaVersions
            .Where(x => x.TransactionSchemaId == transactionSchemaId)
            .MaxAsync(x => (int?)x.VersionNumber, stoppingToken);

        return new VersionNumber(maxVersionNumber ?? 0);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        var exists = await context.TransactionSchemaVersions
            .AnyAsync(x => x.OrganizationId == organizationId && x.VersionNumber == versionNumber, stoppingToken);

        return new Exists(exists);
    }

    protected override Task<List<TransactionSchemaVersion>> MapPageAsync(List<TransactionSchemaVersionEntity> items, ListTransactionSchemaVersionRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<TransactionSchemaVersionEntity> OrderByExpression()
    {
        return new OrderByExpression<TransactionSchemaVersionEntity, int>(x => x.VersionNumber);
    }

    protected override IQueryable<TransactionSchemaVersionEntity> FilterByPagedRequest(ListTransactionSchemaVersionRequest request)
    {
        return context.TransactionSchemaVersions
            .Where(x => x.OrganizationId == request.OrganizationId);
    }

    protected override IQueryable<TransactionSchemaVersionEntity> FilterByDateRange(IQueryable<TransactionSchemaVersionEntity> queryable, DatedRequest datedRequest)
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
