using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class OrganizationDataKeyConfigurations : IEntityTypeConfiguration<OrganizationDataKeyEntity>
{
    public void Configure(EntityTypeBuilder<OrganizationDataKeyEntity> builder)
    {
        builder.HasKey(x => x.OrganizationId);

        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();

        builder.Property(x => x.WrappedDataKey).HasMaxLength(WrappedDataKey.MaxLength).IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.OrganizationDataKeys, Schemas.Service);
    }
}
