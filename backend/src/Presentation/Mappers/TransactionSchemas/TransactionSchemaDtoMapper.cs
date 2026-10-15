using Core.Models.TransactionSchemas;
using Presentation.Models.TransactionSchemas;

namespace Presentation.Mappers.TransactionSchemas;

public static class TransactionSchemaDtoMapper
{
    public static TransactionSchemaResponseDto ToDto(this TransactionSchema transactionSchema)
    {
        return new TransactionSchemaResponseDto
        {
            Mode = transactionSchema.Mode.ToString(),
            TransactionSchemaId = transactionSchema.TransactionSchemaId,
            OrganizationId = transactionSchema.OrganizationId,
            Fields = transactionSchema.Fields.ToDto(),
            CurrentVersionNumber = transactionSchema.CurrentVersionNumber,
            IsDeleted = transactionSchema.IsDeleted,
            DeletedAt = transactionSchema.DeletedAt,
            CreatedAt = transactionSchema.CreatedAt,
            UpdatedAt = transactionSchema.UpdatedAt
        };
    }

    public static TransactionSchemaVersionResponseDto ToDto(this TransactionSchemaVersion transactionSchemaVersion)
    {
        return new TransactionSchemaVersionResponseDto
        {
            Mode = transactionSchemaVersion.Mode.ToString(),
            TransactionSchemaVersionId = transactionSchemaVersion.TransactionSchemaVersionId,
            TransactionSchemaId = transactionSchemaVersion.TransactionSchemaId,
            OrganizationId = transactionSchemaVersion.OrganizationId,
            VersionNumber = transactionSchemaVersion.VersionNumber,
            Fields = transactionSchemaVersion.Fields.ToDto(),
            CreatedAt = transactionSchemaVersion.CreatedAt
        };
    }
}
