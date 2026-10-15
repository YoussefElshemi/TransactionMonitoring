using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ServiceCreditConfigurations : IEntityTypeConfiguration<ServiceCreditEntity>
{
    public void Configure(EntityTypeBuilder<ServiceCreditEntity> builder)
    {
        builder.HasKey(x => x.ServiceCreditId);

        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.AvailabilityCommitment).WithMany().HasForeignKey(x => x.AvailabilityCommitmentId).IsRequired();
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).IsRequired();
        builder.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId);
        builder.HasOne(x => x.CreditNote).WithMany().HasForeignKey(x => x.CreditNoteId);

        builder.HasIndex(x => new { x.OrganizationId, x.Year, x.Month }).IsUnique();

        builder.Property(x => x.AvailabilityTarget).HasPrecision(9, 4);
        builder.Property(x => x.MeasuredUptimePercentage).HasPrecision(9, 4);
        builder.Property(x => x.CreditPercentage).HasPrecision(9, 4);
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.ServiceCredits, Schemas.Service);
    }
}
