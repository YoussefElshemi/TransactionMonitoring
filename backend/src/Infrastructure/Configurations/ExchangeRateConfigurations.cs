using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ExchangeRateConfigurations : IEntityTypeConfiguration<ExchangeRateEntity>
{
    public void Configure(EntityTypeBuilder<ExchangeRateEntity> builder)
    {
        builder.HasKey(x => x.ExchangeRateId);

        builder.HasOne(x => x.FromCurrency).WithMany().HasForeignKey(x => x.FromCurrencyId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ToCurrency).WithMany().HasForeignKey(x => x.ToCurrencyId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);

        builder.HasIndex(x => new { x.FromCurrencyId, x.ToCurrencyId, x.EffectiveFrom });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.ExchangeRates, Schemas.Service);
    }
}
