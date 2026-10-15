using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Common;
using Core.Models.Transactions;
using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TransactionRepository(
    TransactionMonitoringDbContext context,
    IPayloadProtectionService payloadProtectionService) : PagedRepository<ListTransactionRequest, TransactionEntity, Transaction>, ITransactionRepository
{
    public async Task<Transaction?> TryAddAsync(Transaction transaction, CancellationToken stoppingToken)
    {
        var dataKey = await payloadProtectionService.GetOrCreateDataKeyAsync(transaction.OrganizationId, stoppingToken);
        var entry = context.Transactions.Add(transaction.ToEntity(dataKey));

        try
        {
            await context.SaveChangesAsync(stoppingToken);

            return null;
        }
        catch (DbUpdateException)
        {
            entry.State = EntityState.Detached;

            if (!transaction.ExternalReference.HasValue)
            {
                throw;
            }

            var winner = await FindByExternalReferenceAsync(transaction.OrganizationId, transaction.ExternalReference.Value, stoppingToken);

            if (winner is null)
            {
                throw;
            }

            return winner;
        }
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, TransactionId transactionId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.Transactions
            .AnyAsync(x => x.TransactionId == transactionId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<Transaction> GetAsync(TransactionId transactionId, CancellationToken stoppingToken)
    {
        var transactionEntity = await Detail()
            .FirstAsync(x => x.TransactionId == transactionId && !x.IsDeleted, stoppingToken);

        var dataKey = await payloadProtectionService.FindDataKeyAsync(
            new OrganizationId(transactionEntity.OrganizationId), stoppingToken);

        return transactionEntity.ToModel(dataKey);
    }

    public async Task<List<Transaction>> ListByIdsAsync(IReadOnlyCollection<TransactionId> transactionIds, CancellationToken stoppingToken)
    {
        if (transactionIds.Count == 0)
        {
            return [];
        }

        var transactionIdValues = transactionIds.Select(x => (Guid)x).Distinct().ToList();

        // Not Detail(): callers enrich a sibling record, and pulling the case graph would re-fetch what they're enriching.
        var transactionEntities = await context.Transactions
            .Where(x => transactionIdValues.Contains(x.TransactionId) && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        var dataKeys = await payloadProtectionService.FindDataKeysAsync(
            transactionEntities.ConvertAll(x => new OrganizationId(x.OrganizationId)), stoppingToken);

        return transactionEntities.ConvertAll(x => x.ToModel(DataKeyOf(dataKeys, x.OrganizationId)));
    }

    public async Task<Transaction?> FindByExternalReferenceAsync(OrganizationId organizationId, ExternalReference externalReference, CancellationToken stoppingToken)
    {
        string externalReferenceValue = externalReference;

        var transactionEntity = await context.Transactions
            .FirstOrDefaultAsync(x => x.ExternalReference == externalReferenceValue &&
                                      x.OrganizationId == organizationId &&
                                      !x.IsDeleted, stoppingToken);

        var dataKey = await payloadProtectionService.FindDataKeyAsync(organizationId, stoppingToken);

        return transactionEntity?.ToModel(dataKey);
    }

    public async Task<FriendlyName> GenerateFriendlyName(CancellationToken stoppingToken)
    {
        var result = await context.Database
            .SqlQueryRaw<int>($"SELECT nextval('\"{Schemas.Service}\".\"{Sequences.TransactionNumber}\"') as \"Value\"")
            .FirstAsync(stoppingToken);

        return new FriendlyName("TX" + result.ToString("D10"));
    }

    protected override async Task<List<Transaction>> MapPageAsync(List<TransactionEntity> items, ListTransactionRequest request, CancellationToken stoppingToken)
    {
        var dataKeys = await payloadProtectionService.FindDataKeysAsync(
            items.ConvertAll(x => new OrganizationId(x.OrganizationId)), stoppingToken);

        return items.ConvertAll(x => x.ToModel(DataKeyOf(dataKeys, x.OrganizationId)));
    }

    // Misses rather than throws so one legacy row with no key can't fail the page.
    private static DataKey? DataKeyOf(IReadOnlyDictionary<OrganizationId, DataKey> dataKeys, Guid organizationId)
    {
        return dataKeys.TryGetValue(new OrganizationId(organizationId), out var dataKey) ? dataKey : null;
    }

    protected override IOrderByExpression<TransactionEntity> OrderByExpression()
    {
        return new OrderByExpression<TransactionEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<TransactionEntity> FilterByPagedRequest(ListTransactionRequest listTransactionRequest)
    {
        var queryable = context.Transactions
            .Include(x => x.Decision)
            .ThenInclude(x => x!.Case)
            .Where(x => x.OrganizationId == listTransactionRequest.OrganizationId && !x.IsDeleted);

        if (listTransactionRequest.FriendlyName.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.FriendlyName, $"%{listTransactionRequest.FriendlyName}%"));
        }

        if (listTransactionRequest.ExternalReference.HasValue)
        {
            queryable = queryable
                .Where(x => x.ExternalReference != null && EF.Functions.ILike(x.ExternalReference, $"%{listTransactionRequest.ExternalReference}%"));
        }

        return queryable;
    }

    protected override IQueryable<TransactionEntity> FilterByDateRange(IQueryable<TransactionEntity> queryable, DatedRequest datedRequest)
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

    private IQueryable<TransactionEntity> Detail()
    {
        return context.Transactions
            .Include(x => x.RuleEvaluations.Where(evaluation => !evaluation.IsDeleted))
            .Include(x => x.Cases.Where(@case => !@case.IsDeleted))
            .Include(x => x.TransactionParties)
            .ThenInclude(x => x.Party)
            .Include(x => x.Decision)
            .ThenInclude(x => x!.Case);
    }
}
