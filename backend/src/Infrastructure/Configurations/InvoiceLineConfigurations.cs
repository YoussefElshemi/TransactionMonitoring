using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class InvoiceLineConfigurations : IEntityTypeConfiguration<InvoiceLineEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceLineEntity> builder)
    {
        builder.HasKey(x => x.InvoiceLineId);

        builder.HasOne(x => x.Invoice).WithMany(x => x.Lines).HasForeignKey(x => x.InvoiceId).IsRequired();
        builder.HasOne(x => x.InvoiceLineType).WithMany().HasForeignKey(x => x.InvoiceLineTypeId).IsRequired();
        builder.HasOne(x => x.BillableResource).WithMany().HasForeignKey(x => x.BillableResourceId);

        builder.HasOne<InvoiceAdjustmentEntity>()
            .WithMany()
            .HasForeignKey(x => x.InvoiceAdjustmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Description).HasMaxLength(InvoiceLineDescription.MaxLength);

        builder.HasIndex(x => new { x.InvoiceId, x.SortOrder }).IsUnique();

        builder.ToTable(TableNames.InvoiceLines, Schemas.Service);
    }
}
