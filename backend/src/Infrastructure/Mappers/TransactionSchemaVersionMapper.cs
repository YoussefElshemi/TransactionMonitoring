using Core.Enums;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class TransactionSchemaVersionMapper
{
    public static TransactionSchemaVersion ToModel(this TransactionSchemaVersionEntity transactionSchemaVersionEntity)
    {
        return new TransactionSchemaVersion
        {
            Mode = (Mode)transactionSchemaVersionEntity.ModeId,
            TransactionSchemaVersionId = new TransactionSchemaVersionId(transactionSchemaVersionEntity.TransactionSchemaVersionId),
            TransactionSchemaId = new TransactionSchemaId(transactionSchemaVersionEntity.TransactionSchemaId),
            OrganizationId = new OrganizationId(transactionSchemaVersionEntity.OrganizationId),
            VersionNumber = new VersionNumber(transactionSchemaVersionEntity.VersionNumber),
            Fields = transactionSchemaVersionEntity.Fields.FieldsToModel(),
            CreatedByUserId = new ApplicationUserId(transactionSchemaVersionEntity.CreatedByUserId),
            CreatedAt = new CreatedAt(transactionSchemaVersionEntity.CreatedAt)
        };
    }

    public static TransactionSchemaVersionEntity ToEntity(this TransactionSchemaVersion transactionSchemaVersion)
    {
        return new TransactionSchemaVersionEntity
        {
            ModeId = (int)transactionSchemaVersion.Mode,
            TransactionSchemaVersionId = transactionSchemaVersion.TransactionSchemaVersionId,
            TransactionSchemaId = transactionSchemaVersion.TransactionSchemaId,
            OrganizationId = transactionSchemaVersion.OrganizationId,
            VersionNumber = transactionSchemaVersion.VersionNumber,
            Fields = transactionSchemaVersion.Fields.ToBase64String(),
            CreatedByUserId = transactionSchemaVersion.CreatedByUserId,
            CreatedAt = transactionSchemaVersion.CreatedAt
        };
    }
}
