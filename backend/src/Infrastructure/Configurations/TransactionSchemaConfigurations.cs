using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TransactionSchemaConfigurations : IEntityTypeConfiguration<TransactionSchemaEntity>
{
    public void Configure(EntityTypeBuilder<TransactionSchemaEntity> builder)
    {
        builder.HasKey(x => x.TransactionSchemaId);

        builder.HasOne(x => x.Organization).WithMany(x => x.TransactionSchemas).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.TransactionSchemas).HasForeignKey(x => x.CreatedByUserId).IsRequired();
        builder.HasMany(x => x.Versions).WithOne(x => x.TransactionSchema).HasForeignKey(x => x.TransactionSchemaId).IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.ModeId }).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.TransactionSchemas, Schemas.Service);
    }
}
