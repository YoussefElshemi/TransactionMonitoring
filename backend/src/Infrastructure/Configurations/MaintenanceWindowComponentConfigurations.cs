using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class MaintenanceWindowComponentConfigurations : IEntityTypeConfiguration<MaintenanceWindowComponentEntity>
{
    public void Configure(EntityTypeBuilder<MaintenanceWindowComponentEntity> builder)
    {
        builder.HasKey(x => new { x.MaintenanceWindowId, x.ComponentId });

        builder.HasOne(x => x.Component).WithMany().HasForeignKey(x => x.ComponentId).IsRequired();

        builder.HasIndex(x => x.ComponentId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.MaintenanceWindowComponents, Schemas.Service);
    }
}
