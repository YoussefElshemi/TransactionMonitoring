using System.Text.Json.Nodes;
using Core.Exceptions;
using Core.Extensions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Transactions;
using Core.ValueObjects;

namespace Core.Services;

public class TransactionService(
    ITransactionRepository transactionRepository,
    TimeProvider timeProvider,
    IModeContext modeContext) : ITransactionService
{
    public async Task<Transaction> GetOrCreateAsync(CreateTransactionRequest createTransactionRequest, CancellationToken stoppingToken)
    {
        if (createTransactionRequest.ExternalReference.HasValue)
        {
            var existing = await transactionRepository.FindByExternalReferenceAsync(
                createTransactionRequest.OrganizationId,
                createTransactionRequest.ExternalReference.Value,
                stoppingToken);

            if (existing is not null)
            {
                return Reuse(existing, createTransactionRequest.ExternalReference.Value, createTransactionRequest.Payload);
            }
        }

        var transaction = createTransactionRequest.ToModel(timeProvider, modeContext.CurrentOrLive);
        var friendlyName = await transactionRepository.GenerateFriendlyName(stoppingToken);
        transaction = transaction with
        {
            FriendlyName = friendlyName
        };

        var winner = await transactionRepository.TryAddAsync(transaction, stoppingToken);

        return winner is null
            ? transaction
            : Reuse(winner, createTransactionRequest.ExternalReference!.Value, createTransactionRequest.Payload);
    }

    public Task<Transaction> GetAsync(TransactionId transactionId, CancellationToken stoppingToken)
    {
        return transactionRepository.GetAsync(transactionId, stoppingToken);
    }

    public Task<List<Transaction>> ListByIdsAsync(IReadOnlyCollection<TransactionId> transactionIds, CancellationToken stoppingToken)
    {
        return transactionRepository.ListByIdsAsync(transactionIds, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, TransactionId transactionId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return transactionRepository.ExistsAsync(organizationId, transactionId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<Transaction>> ListAsync(ListTransactionRequest listTransactionRequest, CancellationToken stoppingToken)
    {
        return transactionRepository.ListAsync(listTransactionRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListTransactionRequest listTransactionRequest, CancellationToken stoppingToken)
    {
        return transactionRepository.GetCountAsync(listTransactionRequest, stoppingToken);
    }

    private static Transaction Reuse(Transaction existing, ExternalReference externalReference, JsonObject payload)
    {
        if (existing.Payload.Serialize() != payload.Serialize())
        {
            throw new TransactionPayloadMismatchException(externalReference);
        }

        return existing;
    }
}
