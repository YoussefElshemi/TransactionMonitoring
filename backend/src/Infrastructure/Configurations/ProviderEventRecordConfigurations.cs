using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ProviderEventRecordConfigurations : IEntityTypeConfiguration<ProviderEventRecordEntity>
{
    public void Configure(EntityTypeBuilder<ProviderEventRecordEntity> builder)
    {
        builder.HasKey(x => x.ProviderEventRecordId);

        builder.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId);

        builder.HasIndex(x => x.ProviderEventReference).IsUnique();

        builder.HasIndex(x => new { x.IsProcessed, x.CreatedAt });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.ProviderEventRecords, Schemas.Service);
    }
}
