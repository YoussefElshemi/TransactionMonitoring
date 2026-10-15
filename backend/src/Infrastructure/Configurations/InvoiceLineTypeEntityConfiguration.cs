using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class InvoiceLineTypeEntityConfiguration : IEntityTypeConfiguration<InvoiceLineTypeEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceLineTypeEntity> builder)
    {
        builder.HasKey(x => x.InvoiceLineTypeId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.InvoiceLineTypes);

        builder.ToTable(TableNames.InvoiceLineTypes, Schemas.Service);
    }
}
