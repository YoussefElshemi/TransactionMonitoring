using Core.Enums;
using Core.Helpers;
using Core.Models.Transactions;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class TransactionMapper
{
    public static Transaction ToModel(this TransactionEntity transactionEntity, DataKey? dataKey)
    {
        ExternalReference? externalReference = transactionEntity.ExternalReference is null ? null : new ExternalReference(transactionEntity.ExternalReference);

        return new Transaction
        {
            Mode = (Mode)transactionEntity.ModeId,
            TransactionId = new TransactionId(transactionEntity.TransactionId),
            OrganizationId = new OrganizationId(transactionEntity.OrganizationId),
            FriendlyName = new FriendlyName(transactionEntity.FriendlyName),
            ExternalReference = externalReference,
            Payload = PayloadProtector.Read(dataKey, new OrganizationId(transactionEntity.OrganizationId), transactionEntity.Payload),
            RuleEvaluations = transactionEntity.RuleEvaluations.Select(x => x.ToModel()).ToList(),
            Cases = transactionEntity.Cases.Select(x => x.ToModel()).ToList(),
            TransactionParties = transactionEntity.TransactionParties.Select(x => x.ToModel()).ToList(),
            Decision = transactionEntity.Decision?.ToModel(externalReference),
            CreatedAt = new CreatedAt(transactionEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(transactionEntity.UpdatedAt),
            IsDeleted = new IsDeleted(transactionEntity.IsDeleted),
            DeletedAt = transactionEntity.DeletedAt.HasValue ? new DeletedAt(transactionEntity.DeletedAt.Value) : null
        };
    }

    public static TransactionEntity ToEntity(this Transaction transaction, DataKey dataKey)
    {
        return new TransactionEntity
        {
            ModeId = (int)transaction.Mode,
            TransactionId = transaction.TransactionId,
            OrganizationId = transaction.OrganizationId,
            FriendlyName = transaction.FriendlyName,
            ExternalReference = transaction.ExternalReference,
            Payload = PayloadProtector.Write(dataKey, transaction.OrganizationId, transaction.Payload),
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
            IsDeleted = transaction.IsDeleted,
            DeletedAt = transaction.DeletedAt
        };
    }
}
