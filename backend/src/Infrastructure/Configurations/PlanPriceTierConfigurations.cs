using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PlanPriceTierConfigurations : IEntityTypeConfiguration<PlanPriceTierEntity>
{
    public void Configure(EntityTypeBuilder<PlanPriceTierEntity> builder)
    {
        builder.HasKey(x => x.PlanPriceTierId);

        builder.HasOne(x => x.PlanPrice).WithMany(x => x.Tiers).HasForeignKey(x => x.PlanPriceId).IsRequired();

        builder.HasIndex(x => new { x.PlanPriceId, x.FromQuantity }).IsUnique();

        builder.ToTable(TableNames.PlanPriceTiers, Schemas.Service);
    }
}
