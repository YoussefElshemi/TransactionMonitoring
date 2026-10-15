using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class InvoiceAdjustmentConfigurations : IEntityTypeConfiguration<InvoiceAdjustmentEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceAdjustmentEntity> builder)
    {
        builder.HasKey(x => x.InvoiceAdjustmentId);

        builder.HasOne(x => x.Organization).WithMany(x => x.InvoiceAdjustments).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.InvoiceAdjustments).HasForeignKey(x => x.CreatedByUserId);

        builder.HasOne(x => x.AppliedToInvoice)
            .WithMany(x => x.Adjustments)
            .HasForeignKey(x => x.AppliedToInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Description).HasMaxLength(InvoiceAdjustmentDescription.MaxLength);

        builder.HasIndex(x => new { x.OrganizationId, x.Year, x.Month });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.InvoiceAdjustments, Schemas.Service);
    }
}
