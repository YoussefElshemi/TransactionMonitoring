using Core.Enums;
using Core.Models.Transactions;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateTransactionRequestMapper
{
    public static Transaction ToModel(this CreateTransactionRequest createTransactionRequest, TimeProvider timeProvider, Mode mode)
    {
        return new Transaction
        {
            Mode = mode,
            TransactionId = new TransactionId(Guid.NewGuid()),
            OrganizationId = createTransactionRequest.OrganizationId,
            FriendlyName = default,
            ExternalReference = createTransactionRequest.ExternalReference,
            Payload = createTransactionRequest.Payload,
            RuleEvaluations = [],
            Cases = [],
            TransactionParties = [],
            Decision = null,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
