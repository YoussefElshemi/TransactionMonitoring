using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ServiceCostRateConfigurations : IEntityTypeConfiguration<ServiceCostRateEntity>
{
    public void Configure(EntityTypeBuilder<ServiceCostRateEntity> builder)
    {
        builder.HasKey(x => x.ServiceCostRateId);

        builder.HasOne(x => x.BillableResource).WithMany().HasForeignKey(x => x.BillableResourceId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);

        builder.HasIndex(x => new { x.BillableResourceId, x.EffectiveFrom });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.ServiceCostRates, Schemas.Service);
    }
}
