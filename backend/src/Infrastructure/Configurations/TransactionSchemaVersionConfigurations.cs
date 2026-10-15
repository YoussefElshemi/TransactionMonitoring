using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TransactionSchemaVersionConfigurations : IEntityTypeConfiguration<TransactionSchemaVersionEntity>
{
    public void Configure(EntityTypeBuilder<TransactionSchemaVersionEntity> builder)
    {
        builder.HasKey(x => x.TransactionSchemaVersionId);

        builder.HasIndex(x => new { x.TransactionSchemaId, x.VersionNumber }).IsUnique();

        builder.ToTable(TableNames.TransactionSchemaVersions, Schemas.Service);
    }
}
