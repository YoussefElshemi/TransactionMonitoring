using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ContractDiscountConfigurations : IEntityTypeConfiguration<ContractDiscountEntity>
{
    public void Configure(EntityTypeBuilder<ContractDiscountEntity> builder)
    {
        builder.HasKey(x => x.ContractDiscountId);

        builder.HasOne(x => x.Organization).WithMany(x => x.ContractDiscounts).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.DiscountType).WithMany().HasForeignKey(x => x.DiscountTypeId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.ContractDiscounts).HasForeignKey(x => x.CreatedByUserId);

        builder.Property(x => x.Description).HasMaxLength(ContractDiscountDescription.MaxLength);

        builder.HasIndex(x => new { x.OrganizationId, x.EffectiveFrom });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.ContractDiscounts, Schemas.Service);
    }
}
