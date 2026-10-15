using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AuditOperationConfigurations : IEntityTypeConfiguration<AuditOperationEntity>
{
    public void Configure(EntityTypeBuilder<AuditOperationEntity> builder)
    {
        builder.HasKey(x => x.AuditOperationId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.AuditOperations);

        builder.ToTable(TableNames.AuditOperations, Schemas.Service);
    }
}
