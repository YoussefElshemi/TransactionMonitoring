using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Cases;
using Core.Models.Common;
using Core.Models.Parties;
using Core.Models.Transactions;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Helpers;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PartyRepository(
    TransactionMonitoringDbContext context,
    IPayloadProtectionService payloadProtectionService,
    TimeProvider timeProvider) : PagedRepository<ListPartyRequest, PartyEntity, Party>, IPartyRepository
{
    public async Task<Party?> TryAddAsync(Party party, CancellationToken stoppingToken)
    {
        var entry = context.Parties.Add(party.ToEntity());

        try
        {
            await context.SaveChangesAsync(stoppingToken);

            return null;
        }
        catch (DbUpdateException)
        {
            entry.State = EntityState.Detached;

            var winner = await FindByIdentifierAsync(party.OrganizationId, party.Identifier, stoppingToken);

            if (winner is null)
            {
                throw;
            }

            return winner;
        }
    }

    public async Task<Party?> FindByIdentifierAsync(OrganizationId organizationId, PartyIdentifier identifier, CancellationToken stoppingToken)
    {
        string identifierValue = identifier;

        var partyEntity = await context.Parties
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Identifier == identifierValue, stoppingToken);

        return partyEntity?.ToModel([]);
    }

    public async Task<List<Party>> ListByIdentifiersAsync(OrganizationId organizationId, IReadOnlyCollection<PartyIdentifier> identifiers, CancellationToken stoppingToken)
    {
        if (identifiers.Count == 0)
        {
            return [];
        }

        var identifierValues = identifiers.Select(x => (string)x).Distinct().ToList();

        var partyEntities = await context.Parties
            .Where(x => x.OrganizationId == organizationId && identifierValues.Contains(x.Identifier))
            .ToListAsync(stoppingToken);

        return partyEntities.ConvertAll(x => x.ToModel([]));
    }

    public async Task<Party> GetAsync(PartyId partyId, CancellationToken stoppingToken)
    {
        var partyEntity = await context.Parties.FirstAsync(x => x.PartyId == partyId, stoppingToken);

        var roles = await RolesByPartyAsync([partyId], stoppingToken);

        return partyEntity.ToModel(roles.GetValueOrDefault(partyEntity.PartyId, []));
    }

    private async Task<Dictionary<Guid, List<PartyRole>>> RolesByPartyAsync(List<Guid> partyIds, CancellationToken stoppingToken)
    {
        if (partyIds.Count == 0)
        {
            return [];
        }

        var pairs = await context.TransactionParties
            .Where(x => partyIds.Contains(x.PartyId))
            .Select(x => new { x.PartyId, x.PartyRoleId })
            .Distinct()
            .ToListAsync(stoppingToken);

        return pairs
            .GroupBy(x => x.PartyId)
            .ToDictionary(x => x.Key, x => x.Select(pair => (PartyRole)pair.PartyRoleId).Order().ToList());
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, PartyId partyId, CancellationToken stoppingToken)
    {
        var exists = await context.Parties
            .AnyAsync(x => x.PartyId == partyId && x.OrganizationId == organizationId, stoppingToken);

        return new Exists(exists);
    }

    public async Task RecordSightingAsync(
        PartyId partyId,
        PartyDisplayName? displayName,
        List<PartyAttributeValue> attributes,
        LastSeenAt lastSeenAt,
        CancellationToken stoppingToken)
    {
        var partyEntity = await context.Parties.FirstAsync(x => x.PartyId == partyId, stoppingToken);

        if (displayName.HasValue)
        {
            partyEntity.DisplayName = displayName.Value;
        }

        if (attributes.Count > 0)
        {
            partyEntity.Attributes = attributes.ToBase64String();
        }

        partyEntity.LastSeenAt = lastSeenAt;
        partyEntity.UpdatedAt = timeProvider.GetUtcNow();

        var entry = context.Parties.Update(partyEntity);

        try
        {
            await context.SaveChangesAsync(stoppingToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            entry.State = EntityState.Detached;
        }
    }

    public async Task SetRiskAsync(SetPartyRiskRequest setPartyRiskRequest, CancellationToken stoppingToken)
    {
        var partyEntity = await context.Parties
            .FirstAsync(x => x.PartyId == setPartyRiskRequest.PartyId && x.OrganizationId == setPartyRiskRequest.OrganizationId, stoppingToken);

        partyEntity.RiskScore = setPartyRiskRequest.RiskScore;
        partyEntity.RiskNote = setPartyRiskRequest.Note;
        partyEntity.RiskSetByUserId = setPartyRiskRequest.SetByUserId;
        partyEntity.RiskSetAt = timeProvider.GetUtcNow();
        partyEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Parties.Update(partyEntity);
    }

    public async Task<PagedResponse<Transaction>> ListTransactionsAsync(ListPartyTransactionRequest listPartyTransactionRequest, CancellationToken stoppingToken)
    {
        var queryable = FilterTransactions(listPartyTransactionRequest);

        var totalCount = new TotalCount(await queryable.CountAsync(stoppingToken));

        var transactionEntities = await queryable
            .OrderByDescending(x => x.CreatedAt)
            .Skip((listPartyTransactionRequest.PageNumber - 1) * listPartyTransactionRequest.PageSize)
            .Take(listPartyTransactionRequest.PageSize)
            .ToListAsync(stoppingToken);

        var dataKey = await payloadProtectionService.FindDataKeyAsync(listPartyTransactionRequest.OrganizationId, stoppingToken);

        return Paginate(listPartyTransactionRequest, totalCount, transactionEntities.ConvertAll(x => x.ToModel(dataKey)));
    }

    public async Task<TotalCount> GetTransactionCountAsync(ListPartyTransactionRequest listPartyTransactionRequest, CancellationToken stoppingToken)
    {
        return new TotalCount(await FilterTransactions(listPartyTransactionRequest).CountAsync(stoppingToken));
    }

    public async Task<PagedResponse<Case>> ListCasesAsync(ListPartyCaseRequest listPartyCaseRequest, CancellationToken stoppingToken)
    {
        var queryable = FilterCases(listPartyCaseRequest);

        var totalCount = new TotalCount(await queryable.CountAsync(stoppingToken));

        var caseEntities = await queryable
            .OrderByDescending(x => x.CreatedAt)
            .Skip((listPartyCaseRequest.PageNumber - 1) * listPartyCaseRequest.PageSize)
            .Take(listPartyCaseRequest.PageSize)
            .ToListAsync(stoppingToken);

        return Paginate(listPartyCaseRequest, totalCount, caseEntities.ConvertAll(x => x.ToModel()));
    }

    public async Task<TotalCount> GetCaseCountAsync(ListPartyCaseRequest listPartyCaseRequest, CancellationToken stoppingToken)
    {
        return new TotalCount(await FilterCases(listPartyCaseRequest).CountAsync(stoppingToken));
    }

    protected override async Task<List<Party>> MapPageAsync(List<PartyEntity> items, ListPartyRequest request, CancellationToken stoppingToken)
    {
        var roles = await RolesByPartyAsync(items.ConvertAll(x => x.PartyId), stoppingToken);

        return items.ConvertAll(x => x.ToModel(roles.GetValueOrDefault(x.PartyId, [])));
    }

    protected override IOrderByExpression<PartyEntity> OrderByExpression()
    {
        return new OrderByExpression<PartyEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<PartyEntity> FilterByPagedRequest(ListPartyRequest listPartyRequest)
    {
        var queryable = context.Parties
            .Where(x => x.OrganizationId == listPartyRequest.OrganizationId);

        if (listPartyRequest.Identifier.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.Identifier, $"%{listPartyRequest.Identifier}%"));
        }

        if (listPartyRequest.DisplayName.HasValue)
        {
            queryable = queryable
                .Where(x => x.DisplayName != null && EF.Functions.ILike(x.DisplayName, $"%{listPartyRequest.DisplayName}%"));
        }

        if (listPartyRequest.Query.HasValue)
        {
            var pattern = LikePattern.Containing(listPartyRequest.Query.Value);

            queryable = queryable
                .Where(x => EF.Functions.ILike(x.Identifier, pattern, LikePattern.EscapeCharacter) ||
                            (x.DisplayName != null && EF.Functions.ILike(x.DisplayName, pattern, LikePattern.EscapeCharacter)));
        }

        if (listPartyRequest.Role.HasValue)
        {
            var partyRoleId = (int)listPartyRequest.Role.Value;

            queryable = queryable
                .Where(x => x.TransactionParties.Any(link => link.PartyRoleId == partyRoleId));
        }

        return queryable;
    }

    protected override IQueryable<PartyEntity> FilterByDateRange(IQueryable<PartyEntity> queryable, DatedRequest datedRequest)
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

    private IQueryable<TransactionEntity> FilterTransactions(ListPartyTransactionRequest listPartyTransactionRequest)
    {
        var queryable = context.Transactions
            .Include(x => x.Decision)
            .ThenInclude(x => x!.Case)
            .Where(x => !x.IsDeleted &&
                        x.OrganizationId == listPartyTransactionRequest.OrganizationId &&
                        x.TransactionParties.Any(link => link.PartyId == listPartyTransactionRequest.PartyId));

        if (listPartyTransactionRequest.Role.HasValue)
        {
            var partyRoleId = (int)listPartyTransactionRequest.Role.Value;

            queryable = queryable
                .Where(x => x.TransactionParties.Any(link => link.PartyId == listPartyTransactionRequest.PartyId && link.PartyRoleId == partyRoleId));
        }

        if (listPartyTransactionRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= listPartyTransactionRequest.FromDate);
        }

        if (listPartyTransactionRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= listPartyTransactionRequest.ToDate);
        }

        return queryable;
    }

    private IQueryable<CaseEntity> FilterCases(ListPartyCaseRequest listPartyCaseRequest)
    {
        var queryable = context.Cases
            .Where(x => !x.IsDeleted &&
                        x.OrganizationId == listPartyCaseRequest.OrganizationId &&
                        x.Transaction != null &&
                        x.Transaction.TransactionParties.Any(link => link.PartyId == listPartyCaseRequest.PartyId));

        if (listPartyCaseRequest.Status.HasValue)
        {
            var statusId = (int)listPartyCaseRequest.Status.Value;

            queryable = queryable.Where(x => x.StatusId == statusId);
        }

        if (listPartyCaseRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= listPartyCaseRequest.FromDate);
        }

        if (listPartyCaseRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= listPartyCaseRequest.ToDate);
        }

        return queryable;
    }

    private static PagedResponse<TModel> Paginate<TModel>(PagedRequest pagedRequest, TotalCount totalCount, List<TModel> data)
    {
        return new PagedResponse<TModel>
        {
            PageNumber = pagedRequest.PageNumber,
            PageSize = pagedRequest.PageSize,
            PageCount = pagedRequest.GetPageCount(totalCount),
            TotalCount = totalCount,
            HasMore = new HasMore(pagedRequest.PageNumber * pagedRequest.PageSize < totalCount),
            Data = data
        };
    }
}
