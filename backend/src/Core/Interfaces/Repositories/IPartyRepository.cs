using Core.Models.Cases;
using Core.Models.Common;
using Core.Models.Parties;
using Core.Models.Transactions;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IPartyRepository : IPagedRepository<ListPartyRequest, Party>
{
    Task<Party?> TryAddAsync(Party party, CancellationToken stoppingToken);
    Task<Party?> FindByIdentifierAsync(OrganizationId organizationId, PartyIdentifier identifier, CancellationToken stoppingToken);
    Task<List<Party>> ListByIdentifiersAsync(OrganizationId organizationId, IReadOnlyCollection<PartyIdentifier> identifiers, CancellationToken stoppingToken);
    Task<Party> GetAsync(PartyId partyId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, PartyId partyId, CancellationToken stoppingToken);
    Task RecordSightingAsync(PartyId partyId, PartyDisplayName? displayName, List<PartyAttributeValue> attributes, LastSeenAt lastSeenAt, CancellationToken stoppingToken);
    Task SetRiskAsync(SetPartyRiskRequest setPartyRiskRequest, CancellationToken stoppingToken);
    Task<PagedResponse<Transaction>> ListTransactionsAsync(ListPartyTransactionRequest listPartyTransactionRequest, CancellationToken stoppingToken);
    Task<TotalCount> GetTransactionCountAsync(ListPartyTransactionRequest listPartyTransactionRequest, CancellationToken stoppingToken);
    Task<PagedResponse<Case>> ListCasesAsync(ListPartyCaseRequest listPartyCaseRequest, CancellationToken stoppingToken);
    Task<TotalCount> GetCaseCountAsync(ListPartyCaseRequest listPartyCaseRequest, CancellationToken stoppingToken);
}
