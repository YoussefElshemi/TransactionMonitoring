using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class InvoiceStatusEntityConfiguration : IEntityTypeConfiguration<InvoiceStatusEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceStatusEntity> builder)
    {
        builder.HasKey(x => x.InvoiceStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.InvoiceStatuses);

        builder.ToTable(TableNames.InvoiceStatuses, Schemas.Service);
    }
}
