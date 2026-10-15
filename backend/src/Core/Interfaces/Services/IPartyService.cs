using System.Text.Json.Nodes;
using Core.Models.Cases;
using Core.Models.Common;
using Core.Models.Parties;
using Core.Models.Transactions;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IPartyService : IPagedService<ListPartyRequest, Party>
{
    Task<List<PartyFacts>> RecordAsync(RecordTransactionPartiesRequest recordTransactionPartiesRequest, CancellationToken stoppingToken);
    Task<List<PartyFacts>> PeekAsync(OrganizationId organizationId, JsonObject payload, CancellationToken stoppingToken);
    Task<List<List<PartyFacts>>> PeekManyAsync(OrganizationId organizationId, IReadOnlyList<JsonObject> payloads, CancellationToken stoppingToken);
    Task<Party> SetRiskAsync(SetPartyRiskRequest setPartyRiskRequest, CancellationToken stoppingToken);
    Task<Party> GetAsync(PartyId partyId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, PartyId partyId, CancellationToken stoppingToken);
    Task<List<TransactionParty>> ListByTransactionAsync(TransactionId transactionId, CancellationToken stoppingToken);
    Task<PagedResponse<Transaction>> ListTransactionsAsync(ListPartyTransactionRequest listPartyTransactionRequest, CancellationToken stoppingToken);
    Task<TotalCount> GetTransactionCountAsync(ListPartyTransactionRequest listPartyTransactionRequest, CancellationToken stoppingToken);
    Task<PagedResponse<Case>> ListCasesAsync(ListPartyCaseRequest listPartyCaseRequest, CancellationToken stoppingToken);
    Task<TotalCount> GetCaseCountAsync(ListPartyCaseRequest listPartyCaseRequest, CancellationToken stoppingToken);
}
