using Core.Enums;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;

namespace Core.Mappers;

public static class SetTransactionSchemaRequestMapper
{
    public static TransactionSchema ToModel(this SetTransactionSchemaRequest setTransactionSchemaRequest, TransactionSchemaVersionId currentVersionId, TimeProvider timeProvider, Mode mode)
    {
        return new TransactionSchema
        {
            Mode = mode,
            TransactionSchemaId = new TransactionSchemaId(Guid.NewGuid()),
            OrganizationId = setTransactionSchemaRequest.OrganizationId,
            Fields = setTransactionSchemaRequest.Fields,
            CurrentVersionId = currentVersionId,
            CurrentVersionNumber = new VersionNumber(1),
            CreatedByUserId = setTransactionSchemaRequest.CreatedByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }

    public static TransactionSchemaVersion ToVersionModel(
        this SetTransactionSchemaRequest setTransactionSchemaRequest,
        TransactionSchemaVersionId transactionSchemaVersionId,
        TransactionSchemaId transactionSchemaId,
        VersionNumber versionNumber,
        TimeProvider timeProvider, Mode mode)
    {
        return new TransactionSchemaVersion
        {
            Mode = mode,
            TransactionSchemaVersionId = transactionSchemaVersionId,
            TransactionSchemaId = transactionSchemaId,
            OrganizationId = setTransactionSchemaRequest.OrganizationId,
            VersionNumber = versionNumber,
            Fields = setTransactionSchemaRequest.Fields,
            CreatedByUserId = setTransactionSchemaRequest.CreatedByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow())
        };
    }
}
