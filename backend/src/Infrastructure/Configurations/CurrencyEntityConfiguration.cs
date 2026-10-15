using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CurrencyEntityConfiguration : IEntityTypeConfiguration<CurrencyEntity>
{
    public void Configure(EntityTypeBuilder<CurrencyEntity> builder)
    {
        builder.HasKey(x => x.CurrencyId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.Currencies);

        builder.ToTable(TableNames.Currencies, Schemas.Service);
    }
}
