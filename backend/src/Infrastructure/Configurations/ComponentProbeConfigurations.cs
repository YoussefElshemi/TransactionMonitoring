using Core.Enums;
using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ComponentProbeConfigurations : IEntityTypeConfiguration<ComponentProbeEntity>
{
    public void Configure(EntityTypeBuilder<ComponentProbeEntity> builder)
    {
        builder.HasKey(x => x.ComponentProbeId);

        builder.HasOne(x => x.Component).WithMany().HasForeignKey(x => x.ComponentId).IsRequired();
        builder.HasOne(x => x.Source).WithMany().HasForeignKey(x => x.SourceId).IsRequired();

        // Database default of Internal makes adding the column safe on existing rows.
        builder.Property(x => x.SourceId).HasDefaultValue((int)ProbeSource.Internal);

        builder.HasIndex(x => new { x.ComponentId, x.ObservedAt });
        builder.HasIndex(x => x.ObservedAt);

        builder.Property(x => x.Detail).HasMaxLength(ProbeDetail.MaxLength);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.ComponentProbes, Schemas.Service);
    }
}
