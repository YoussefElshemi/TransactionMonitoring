using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PermissionScopeTypeConfigurations : IEntityTypeConfiguration<PermissionScopeTypeEntity>
{
    public void Configure(EntityTypeBuilder<PermissionScopeTypeEntity> builder)
    {
        builder.HasKey(x => x.PermissionScopeTypeId);

        builder.Property(x => x.Name).HasMaxLength(64).IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.PermissionScopeTypes);

        builder.ToTable(TableNames.PermissionScopeTypes, Schemas.Service);
    }
}
