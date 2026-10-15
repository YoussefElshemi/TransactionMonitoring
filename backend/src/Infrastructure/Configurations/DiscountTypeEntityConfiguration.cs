using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DiscountTypeEntityConfiguration : IEntityTypeConfiguration<DiscountTypeEntity>
{
    public void Configure(EntityTypeBuilder<DiscountTypeEntity> builder)
    {
        builder.HasKey(x => x.DiscountTypeId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.DiscountTypes);

        builder.ToTable(TableNames.DiscountTypes, Schemas.Service);
    }
}
