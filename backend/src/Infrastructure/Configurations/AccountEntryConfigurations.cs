using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AccountEntryConfigurations : IEntityTypeConfiguration<AccountEntryEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntryEntity> builder)
    {
        builder.HasKey(x => x.AccountEntryId);

        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).IsRequired();
        builder.HasOne(x => x.AccountEntryReason).WithMany().HasForeignKey(x => x.AccountEntryReasonId).IsRequired();
        builder.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId);
        builder.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId);
        builder.HasOne(x => x.CreditNote).WithMany().HasForeignKey(x => x.CreditNoteId);

        builder.HasIndex(x => new { x.OrganizationId, x.CurrencyId });

        builder.HasIndex(x => x.InvoiceId).HasFilter("\"InvoiceId\" IS NOT NULL");

        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.AccountEntries, Schemas.Service);
    }
}
