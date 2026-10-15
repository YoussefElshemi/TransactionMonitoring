using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AvailabilityWindowLatencyBucketConfigurations : IEntityTypeConfiguration<AvailabilityWindowLatencyBucketEntity>
{
    public void Configure(EntityTypeBuilder<AvailabilityWindowLatencyBucketEntity> builder)
    {
        builder.HasKey(x => new { x.AvailabilityWindowId, x.UpperBoundMilliseconds });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.AvailabilityWindowLatencyBuckets, Schemas.Service);
    }
}
