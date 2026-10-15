using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class InvoiceNumberSequenceConfigurations : IEntityTypeConfiguration<InvoiceNumberSequenceEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceNumberSequenceEntity> builder)
    {
        builder.HasKey(x => new { x.BillingEntityId, x.Year });

        builder.HasOne(x => x.BillingEntity).WithMany(x => x.NumberSequences).HasForeignKey(x => x.BillingEntityId).IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.InvoiceNumberSequences, Schemas.Service);
    }
}
