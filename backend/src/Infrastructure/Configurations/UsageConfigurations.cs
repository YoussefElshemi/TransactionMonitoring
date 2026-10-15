using Core.Enums;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class UsageConfigurations : IEntityTypeConfiguration<UsageEntity>
{
    public void Configure(EntityTypeBuilder<UsageEntity> builder)
    {
        builder.HasKey(x => x.UsageId);

        builder.HasOne(x => x.Organization).WithMany(x => x.Usages).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.BillableResource).WithMany().HasForeignKey(x => x.BillableResourceId).IsRequired();

        // Rows predating this column land on Unclassified and bill at the catch-all price.
        builder.Property(x => x.BillableResourceId).HasDefaultValue((int)BillableResource.Unclassified);

        builder.HasIndex(x => new { x.OrganizationId, x.BillableResourceId, x.CreatedAt });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Usages, Schemas.Service);
    }
}