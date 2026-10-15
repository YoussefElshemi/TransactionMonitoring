using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AuditableEntityTypeConfigurations : IEntityTypeConfiguration<AuditableEntityTypeEntity>
{
    public void Configure(EntityTypeBuilder<AuditableEntityTypeEntity> builder)
    {
        builder.HasKey(x => x.AuditableEntityTypeId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.AuditableEntityTypes);

        builder.ToTable(TableNames.AuditableEntityTypes, Schemas.Service);
    }
}
