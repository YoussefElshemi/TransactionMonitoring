using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PricingModelEntityConfiguration : IEntityTypeConfiguration<PricingModelEntity>
{
    public void Configure(EntityTypeBuilder<PricingModelEntity> builder)
    {
        builder.HasKey(x => x.PricingModelId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.PricingModels);

        builder.ToTable(TableNames.PricingModels, Schemas.Service);
    }
}
