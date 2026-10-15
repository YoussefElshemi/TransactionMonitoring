using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CreditNoteConfigurations : IEntityTypeConfiguration<CreditNoteEntity>
{
    public void Configure(EntityTypeBuilder<CreditNoteEntity> builder)
    {
        builder.HasKey(x => x.CreditNoteId);

        builder.HasOne(x => x.Invoice).WithMany(x => x.CreditNotes).HasForeignKey(x => x.InvoiceId).IsRequired();
        builder.HasOne(x => x.Organization).WithMany(x => x.CreditNotes).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.BillingEntity).WithMany(x => x.CreditNotes).HasForeignKey(x => x.BillingEntityId).IsRequired();
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).IsRequired();
        builder.HasOne(x => x.TaxTreatment).WithMany().HasForeignKey(x => x.TaxTreatmentId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);

        builder.Navigation(x => x.Invoice).AutoInclude();
        builder.Navigation(x => x.Lines).AutoInclude();

        builder.Property(x => x.CreditNoteNumber).HasMaxLength(CreditNoteNumber.MaxLength);
        builder.Property(x => x.Reason).HasMaxLength(CreditNoteReason.MaxLength);

        builder.HasIndex(x => new { x.BillingEntityId, x.CreditNoteNumber }).IsUnique();

        builder.Property(x => x.Locale).HasMaxLength(Locale.MaxLength);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.CreditNotes, Schemas.Service);
    }
}
