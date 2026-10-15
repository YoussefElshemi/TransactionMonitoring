using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class BillingEntityConfigurations : IEntityTypeConfiguration<BillingEntityEntity>
{
    public void Configure(EntityTypeBuilder<BillingEntityEntity> builder)
    {
        builder.HasKey(x => x.BillingEntityId);

        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);

        builder.Property(x => x.LegalName).HasMaxLength(LegalName.MaxLength);
        builder.Property(x => x.Address).HasMaxLength(PostalAddress.MaxLength);
        builder.Property(x => x.CountryCode).HasMaxLength(CountryCode.Length);
        builder.Property(x => x.TaxIdentifier).HasMaxLength(TaxIdentifier.MaxLength);
        builder.Property(x => x.InvoiceNumberPrefix).HasMaxLength(NumberPrefix.MaxLength);
        builder.Property(x => x.CreditNoteNumberPrefix).HasMaxLength(NumberPrefix.MaxLength);

        builder.HasIndex(x => x.InvoiceNumberPrefix).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.CreditNoteNumberPrefix).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.BillingEntities, Schemas.Service);
    }
}
