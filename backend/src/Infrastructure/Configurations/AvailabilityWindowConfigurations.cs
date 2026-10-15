using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AvailabilityWindowConfigurations : IEntityTypeConfiguration<AvailabilityWindowEntity>
{
    public void Configure(EntityTypeBuilder<AvailabilityWindowEntity> builder)
    {
        builder.HasKey(x => x.AvailabilityWindowId);

        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();

        builder.HasMany(x => x.LatencyBuckets)
            .WithOne(x => x.AvailabilityWindow)
            .HasForeignKey(x => x.AvailabilityWindowId);

        builder.HasIndex(x => new { x.OrganizationId, x.PeriodStart }).IsUnique();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.AvailabilityWindows, Schemas.Service);
    }
}
