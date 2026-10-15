using System.Text.Json.Nodes;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Cases;
using Core.Models.Common;
using Core.Models.Parties;
using Core.Models.Transactions;
using Core.ValueObjects;

namespace Core.Services;

public class PartyService(
    IPartyRepository partyRepository,
    ITransactionPartyRepository transactionPartyRepository,
    IPartyMappingRepository partyMappingRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : IPartyService
{
    public async Task<List<PartyFacts>> RecordAsync(RecordTransactionPartiesRequest recordTransactionPartiesRequest, CancellationToken stoppingToken)
    {
        var resolvedParties = await ResolveAsync(recordTransactionPartiesRequest.OrganizationId, recordTransactionPartiesRequest.Payload, stoppingToken);

        if (resolvedParties.Count == 0)
        {
            return [];
        }

        var partiesByIdentifier = await UpsertAsync(recordTransactionPartiesRequest.OrganizationId, resolvedParties, stoppingToken);

        await LinkAsync(recordTransactionPartiesRequest.TransactionId, resolvedParties, partiesByIdentifier, stoppingToken);

        return Facts(resolvedParties, partiesByIdentifier);
    }

    public async Task<List<PartyFacts>> PeekAsync(OrganizationId organizationId, JsonObject payload, CancellationToken stoppingToken)
    {
        var resolvedParties = await ResolveAsync(organizationId, payload, stoppingToken);

        if (resolvedParties.Count == 0)
        {
            return [];
        }

        var identifiers = resolvedParties.Select(x => x.Identifier).Distinct().ToList();
        var existing = await partyRepository.ListByIdentifiersAsync(organizationId, identifiers, stoppingToken);
        var partiesByIdentifier = existing.ToDictionary(x => (string)x.Identifier, x => x, StringComparer.Ordinal);

        return Facts(resolvedParties, partiesByIdentifier);
    }

    public async Task<List<List<PartyFacts>>> PeekManyAsync(OrganizationId organizationId, IReadOnlyList<JsonObject> payloads, CancellationToken stoppingToken)
    {
        var partyMapping = await partyMappingRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);

        if (partyMapping == null)
        {
            return payloads.Select(_ => new List<PartyFacts>()).ToList();
        }

        var resolvedPerPayload = payloads.Select(x => PartyResolver.Resolve(x, partyMapping.Mappings)).ToList();

        var identifiers = resolvedPerPayload.SelectMany(x => x).Select(x => x.Identifier).Distinct().ToList();
        var existing = await partyRepository.ListByIdentifiersAsync(organizationId, identifiers, stoppingToken);
        var partiesByIdentifier = existing.ToDictionary(x => (string)x.Identifier, x => x, StringComparer.Ordinal);

        return resolvedPerPayload.ConvertAll(x => Facts(x, partiesByIdentifier));
    }

    public async Task<Party> SetRiskAsync(SetPartyRiskRequest setPartyRiskRequest, CancellationToken stoppingToken)
    {
        await partyRepository.SetRiskAsync(setPartyRiskRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await partyRepository.GetAsync(setPartyRiskRequest.PartyId, stoppingToken);
    }

    public Task<Party> GetAsync(PartyId partyId, CancellationToken stoppingToken)
    {
        return partyRepository.GetAsync(partyId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, PartyId partyId, CancellationToken stoppingToken)
    {
        return partyRepository.ExistsAsync(organizationId, partyId, stoppingToken);
    }

    public Task<List<TransactionParty>> ListByTransactionAsync(TransactionId transactionId, CancellationToken stoppingToken)
    {
        return transactionPartyRepository.ListByTransactionAsync(transactionId, stoppingToken);
    }

    public Task<PagedResponse<Party>> ListAsync(ListPartyRequest listPartyRequest, CancellationToken stoppingToken)
    {
        return partyRepository.ListAsync(listPartyRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListPartyRequest listPartyRequest, CancellationToken stoppingToken)
    {
        return partyRepository.GetCountAsync(listPartyRequest, stoppingToken);
    }

    public Task<PagedResponse<Transaction>> ListTransactionsAsync(ListPartyTransactionRequest listPartyTransactionRequest, CancellationToken stoppingToken)
    {
        return partyRepository.ListTransactionsAsync(listPartyTransactionRequest, stoppingToken);
    }

    public Task<TotalCount> GetTransactionCountAsync(ListPartyTransactionRequest listPartyTransactionRequest, CancellationToken stoppingToken)
    {
        return partyRepository.GetTransactionCountAsync(listPartyTransactionRequest, stoppingToken);
    }

    public Task<PagedResponse<Case>> ListCasesAsync(ListPartyCaseRequest listPartyCaseRequest, CancellationToken stoppingToken)
    {
        return partyRepository.ListCasesAsync(listPartyCaseRequest, stoppingToken);
    }

    public Task<TotalCount> GetCaseCountAsync(ListPartyCaseRequest listPartyCaseRequest, CancellationToken stoppingToken)
    {
        return partyRepository.GetCaseCountAsync(listPartyCaseRequest, stoppingToken);
    }

    private async Task<List<ResolvedParty>> ResolveAsync(OrganizationId organizationId, JsonObject payload, CancellationToken stoppingToken)
    {
        var partyMapping = await partyMappingRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);

        return partyMapping == null ? [] : PartyResolver.Resolve(payload, partyMapping.Mappings);
    }

    private static List<PartyFacts> Facts(List<ResolvedParty> resolvedParties, Dictionary<string, Party> partiesByIdentifier)
    {
        var facts = new List<PartyFacts>();

        foreach (var resolvedParty in resolvedParties)
        {
            if (partiesByIdentifier.TryGetValue(resolvedParty.Identifier, out var party))
            {
                facts.Add(party.ToFacts(resolvedParty.Role));
            }
        }

        return facts;
    }

    private async Task<Dictionary<string, Party>> UpsertAsync(
        OrganizationId organizationId,
        List<ResolvedParty> resolvedParties,
        CancellationToken stoppingToken)
    {
        var identifiers = resolvedParties.Select(x => x.Identifier).Distinct().ToList();
        var existing = await partyRepository.ListByIdentifiersAsync(organizationId, identifiers, stoppingToken);

        // Ordinal: a case-insensitive identity would fuse accounts the provider distinguishes.
        var partiesByIdentifier = existing.ToDictionary(x => (string)x.Identifier, x => x, StringComparer.Ordinal);

        // Grouped: one identifier can fill two roles, and adding then updating it in one request makes EF throw.
        foreach (var sighting in resolvedParties.GroupBy(x => (string)x.Identifier, StringComparer.Ordinal))
        {
            var resolvedParty = sighting.First();

            if (partiesByIdentifier.TryGetValue(resolvedParty.Identifier, out var party))
            {
                partiesByIdentifier[resolvedParty.Identifier] = await SeeAgainAsync(party, resolvedParty, stoppingToken);

                continue;
            }

            partiesByIdentifier[resolvedParty.Identifier] = await AddAsync(organizationId, resolvedParty, stoppingToken);
        }

        return partiesByIdentifier;
    }

    private async Task<Party> AddAsync(OrganizationId organizationId, ResolvedParty resolvedParty, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var party = new Party
        {
            Mode = modeContext.CurrentOrLive,
            PartyId = new PartyId(Guid.NewGuid()),
            OrganizationId = organizationId,
            Identifier = resolvedParty.Identifier,
            DisplayName = resolvedParty.DisplayName,
            RiskScore = null,
            RiskNote = null,
            RiskSetByUserId = null,
            RiskSetAt = null,
            FirstSeenAt = new FirstSeenAt(now),
            LastSeenAt = new LastSeenAt(now),
            Attributes = resolvedParty.Attributes,
            Roles = [],
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };

        // Concurrent payments naming the same new party race on the unique index; the loser reads back the winner.
        var winner = await partyRepository.TryAddAsync(party, stoppingToken);

        return winner ?? party;
    }

    private async Task<Party> SeeAgainAsync(Party party, ResolvedParty resolvedParty, CancellationToken stoppingToken)
    {
        var lastSeenAt = new LastSeenAt(timeProvider.GetUtcNow());

        await partyRepository.RecordSightingAsync(
            party.PartyId, resolvedParty.DisplayName, resolvedParty.Attributes, lastSeenAt, stoppingToken);

        return party with
        {
            DisplayName = resolvedParty.DisplayName ?? party.DisplayName,
            Attributes = resolvedParty.Attributes.Count > 0 ? resolvedParty.Attributes : party.Attributes,
            LastSeenAt = lastSeenAt
        };
    }

    // Appended idempotently: re-evaluations and concurrent evaluations of one reference resolve the same links.
    private async Task LinkAsync(
        TransactionId transactionId,
        List<ResolvedParty> resolvedParties,
        Dictionary<string, Party> partiesByIdentifier,
        CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var links = resolvedParties.ConvertAll(resolvedParty =>
        {
            var party = partiesByIdentifier[resolvedParty.Identifier];

            return new TransactionParty
            {
                TransactionId = transactionId,
                PartyId = party.PartyId,
                Role = resolvedParty.Role,
                Party = party,
                CreatedAt = new CreatedAt(now),
                UpdatedAt = new UpdatedAt(now)
            };
        });

        // No save follows: each write owns its commit, since a pending one would hand the retry an already-aborted transaction.
        await transactionPartyRepository.AppendAsync(links, stoppingToken);
    }
}
