using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ServiceCreditBandConfigurations : IEntityTypeConfiguration<ServiceCreditBandEntity>
{
    public void Configure(EntityTypeBuilder<ServiceCreditBandEntity> builder)
    {
        builder.HasKey(x => x.ServiceCreditBandId);

        builder.HasIndex(x => new { x.AvailabilityCommitmentId, x.BelowUptimePercentage }).IsUnique();

        builder.Property(x => x.BelowUptimePercentage).HasPrecision(9, 4);
        builder.Property(x => x.CreditPercentage).HasPrecision(9, 4);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.ServiceCreditBands, Schemas.Service);
    }
}
