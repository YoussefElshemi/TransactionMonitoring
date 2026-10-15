using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PermissionConfigurations : IEntityTypeConfiguration<PermissionEntity>
{
    public void Configure(EntityTypeBuilder<PermissionEntity> builder)
    {
        builder.HasKey(x => x.PermissionId);

        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.Permissions);

        builder.ToTable(TableNames.Permissions, Schemas.Service);
    }
}
