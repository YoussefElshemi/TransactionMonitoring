using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PlanPriceConfigurations : IEntityTypeConfiguration<PlanPriceEntity>
{
    public void Configure(EntityTypeBuilder<PlanPriceEntity> builder)
    {
        builder.HasKey(x => x.PlanPriceId);

        builder.HasOne(x => x.PlanVersion).WithMany(x => x.Prices).HasForeignKey(x => x.PlanVersionId).IsRequired();
        builder.HasOne(x => x.PricingModel).WithMany().HasForeignKey(x => x.PricingModelId).IsRequired();
        builder.HasOne(x => x.BillableResource).WithMany().HasForeignKey(x => x.BillableResourceId);

        builder.Navigation(x => x.Tiers).AutoInclude();

        builder.HasIndex(x => new { x.PlanVersionId, x.BillableResourceId }).IsUnique();

        // Postgres treats nulls as distinct, so the index above doesn't stop two catch-alls.
        builder.HasIndex(x => x.PlanVersionId)
            .IsUnique()
            .HasFilter("\"BillableResourceId\" IS NULL")
            .HasDatabaseName("IX_PlanPrices_PlanVersionId_CatchAll");

        builder.ToTable(TableNames.PlanPrices, Schemas.Service);
    }
}
