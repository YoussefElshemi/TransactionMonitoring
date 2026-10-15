using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PaymentMethodKindEntityConfiguration : IEntityTypeConfiguration<PaymentMethodKindEntity>
{
    public void Configure(EntityTypeBuilder<PaymentMethodKindEntity> builder)
    {
        builder.HasKey(x => x.PaymentMethodKindId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.PaymentMethodKinds);

        builder.ToTable(TableNames.PaymentMethodKinds, Schemas.Service);
    }
}
