using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PaymentConfigurations : IEntityTypeConfiguration<PaymentEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEntity> builder)
    {
        builder.HasKey(x => x.PaymentId);

        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).IsRequired();
        builder.HasOne(x => x.PaymentStatus).WithMany().HasForeignKey(x => x.PaymentStatusId).IsRequired();
        builder.HasOne(x => x.PaymentMethodKind).WithMany().HasForeignKey(x => x.PaymentMethodKindId).IsRequired();
        builder.HasOne(x => x.RaisedForInvoice).WithMany().HasForeignKey(x => x.RaisedForInvoiceId);
        builder.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId);

        builder.Navigation(x => x.Organization).AutoInclude();

        builder.HasIndex(x => x.ProviderPaymentReference).IsUnique().HasFilter("\"ProviderPaymentReference\" IS NOT NULL");

        // Partial: unsettled payments have no number, and a plain unique index would allow only one null.
        builder.HasIndex(x => x.ReceiptNumber).IsUnique().HasFilter("\"ReceiptNumber\" IS NOT NULL");

        builder.HasIndex(x => new { x.OrganizationId, x.ReceivedAt });
        builder.HasIndex(x => x.RaisedForInvoiceId);

        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Payments, Schemas.Service);
    }
}
