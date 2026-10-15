using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ComponentUptimeDayConfigurations : IEntityTypeConfiguration<ComponentUptimeDayEntity>
{
    public void Configure(EntityTypeBuilder<ComponentUptimeDayEntity> builder)
    {
        builder.HasKey(x => x.ComponentUptimeDayId);

        builder.HasOne(x => x.Component).WithMany().HasForeignKey(x => x.ComponentId).IsRequired();

        builder.HasIndex(x => new { x.ComponentId, x.Date }).IsUnique();

        builder.Property(x => x.UptimePercentage).HasPrecision(9, 4);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.ComponentUptimeDays, Schemas.Service);
    }
}
