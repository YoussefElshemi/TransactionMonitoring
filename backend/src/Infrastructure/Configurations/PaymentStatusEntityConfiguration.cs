using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PaymentStatusEntityConfiguration : IEntityTypeConfiguration<PaymentStatusEntity>
{
    public void Configure(EntityTypeBuilder<PaymentStatusEntity> builder)
    {
        builder.HasKey(x => x.PaymentStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.PaymentStatuses);

        builder.ToTable(TableNames.PaymentStatuses, Schemas.Service);
    }
}
