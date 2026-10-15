using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class InvoiceConfigurations : IEntityTypeConfiguration<InvoiceEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceEntity> builder)
    {
        builder.HasKey(x => x.InvoiceId);

        builder.HasOne(x => x.Organization).WithMany(x => x.Invoices).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.Invoices).HasForeignKey(x => x.CreatedByUserId);
        builder.HasOne(x => x.PlanVersion).WithMany(x => x.Invoices).HasForeignKey(x => x.PlanVersionId);
        builder.HasOne(x => x.BillingEntity).WithMany(x => x.Invoices).HasForeignKey(x => x.BillingEntityId);
        builder.HasOne(x => x.InvoiceStatus).WithMany().HasForeignKey(x => x.InvoiceStatusId).IsRequired();
        builder.HasOne(x => x.TaxTreatment).WithMany().HasForeignKey(x => x.TaxTreatmentId).IsRequired();
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PlanCurrency).WithMany().HasForeignKey(x => x.PlanCurrencyId).IsRequired().OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Organization).AutoInclude();
        builder.Navigation(x => x.PlanVersion).AutoInclude();
        builder.Navigation(x => x.Lines).AutoInclude();

        builder.Property(x => x.InvoiceNumber).HasMaxLength(InvoiceNumber.MaxLength);
        builder.Property(x => x.SupplierName).HasMaxLength(LegalName.MaxLength);
        builder.Property(x => x.SupplierAddress).HasMaxLength(PostalAddress.MaxLength);
        builder.Property(x => x.SupplierCountryCode).HasMaxLength(CountryCode.Length);
        builder.Property(x => x.SupplierTaxIdentifier).HasMaxLength(TaxIdentifier.MaxLength);
        builder.Property(x => x.CustomerName).HasMaxLength(LegalName.MaxLength);
        builder.Property(x => x.CustomerAddress).HasMaxLength(PostalAddress.MaxLength);
        builder.Property(x => x.CustomerCountryCode).HasMaxLength(CountryCode.Length);
        builder.Property(x => x.CustomerTaxIdentifier).HasMaxLength(TaxIdentifier.MaxLength);

        // Partial: drafts have no number, and a plain unique index would allow only one per entity.
        builder.HasIndex(x => new { x.BillingEntityId, x.InvoiceNumber })
            .IsUnique()
            .HasFilter("\"InvoiceNumber\" IS NOT NULL");

        builder.HasIndex(x => x.DueAt).HasFilter("\"DueAt\" IS NOT NULL");

        builder.Property(x => x.Locale).HasMaxLength(Locale.MaxLength);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Invoices, Schemas.Service);
    }
}
