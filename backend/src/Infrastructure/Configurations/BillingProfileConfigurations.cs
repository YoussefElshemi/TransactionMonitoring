using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class BillingProfileConfigurations : IEntityTypeConfiguration<BillingProfileEntity>
{
    public void Configure(EntityTypeBuilder<BillingProfileEntity> builder)
    {
        builder.HasKey(x => x.BillingProfileId);

        builder.HasOne(x => x.Organization).WithOne(x => x.BillingProfile).HasForeignKey<BillingProfileEntity>(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.BillingEntity).WithMany(x => x.BillingProfiles).HasForeignKey(x => x.BillingEntityId).IsRequired();
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).IsRequired();
        builder.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId);

        builder.Navigation(x => x.BillingEntity).AutoInclude();

        builder.Property(x => x.LegalName).HasMaxLength(LegalName.MaxLength);
        builder.Property(x => x.ContactName).HasMaxLength(ContactName.MaxLength);
        builder.Property(x => x.Address).HasMaxLength(PostalAddress.MaxLength);
        builder.Property(x => x.CountryCode).HasMaxLength(CountryCode.Length);
        builder.Property(x => x.TaxIdentifier).HasMaxLength(TaxIdentifier.MaxLength);

        builder.HasIndex(x => x.OrganizationId).IsUnique();

        builder.Property(x => x.Locale).HasMaxLength(Locale.MaxLength);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.BillingProfiles, Schemas.Service);
    }
}
