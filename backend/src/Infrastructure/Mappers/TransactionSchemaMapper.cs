using Core.Enums;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class TransactionSchemaMapper
{
    public static TransactionSchema ToModel(this TransactionSchemaEntity transactionSchemaEntity)
    {
        return new TransactionSchema
        {
            Mode = (Mode)transactionSchemaEntity.ModeId,
            TransactionSchemaId = new TransactionSchemaId(transactionSchemaEntity.TransactionSchemaId),
            OrganizationId = new OrganizationId(transactionSchemaEntity.OrganizationId),
            Fields = transactionSchemaEntity.Fields.FieldsToModel(),
            CurrentVersionId = new TransactionSchemaVersionId(transactionSchemaEntity.CurrentVersionId),
            CurrentVersionNumber = new VersionNumber(transactionSchemaEntity.CurrentVersionNumber),
            CreatedByUserId = new ApplicationUserId(transactionSchemaEntity.CreatedByUserId),
            CreatedAt = new CreatedAt(transactionSchemaEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(transactionSchemaEntity.UpdatedAt),
            IsDeleted = new IsDeleted(transactionSchemaEntity.IsDeleted),
            DeletedAt = transactionSchemaEntity.DeletedAt.HasValue ? new DeletedAt(transactionSchemaEntity.DeletedAt.Value) : null
        };
    }

    public static TransactionSchemaEntity ToEntity(this TransactionSchema transactionSchema)
    {
        return new TransactionSchemaEntity
        {
            ModeId = (int)transactionSchema.Mode,
            TransactionSchemaId = transactionSchema.TransactionSchemaId,
            OrganizationId = transactionSchema.OrganizationId,
            Fields = transactionSchema.Fields.ToBase64String(),
            CurrentVersionId = transactionSchema.CurrentVersionId,
            CurrentVersionNumber = transactionSchema.CurrentVersionNumber,
            CreatedByUserId = transactionSchema.CreatedByUserId,
            CreatedAt = transactionSchema.CreatedAt,
            UpdatedAt = transactionSchema.UpdatedAt,
            IsDeleted = transactionSchema.IsDeleted,
            DeletedAt = transactionSchema.DeletedAt
        };
    }
}
