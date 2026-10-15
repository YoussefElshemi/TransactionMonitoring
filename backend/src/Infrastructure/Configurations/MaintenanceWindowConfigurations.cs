using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class MaintenanceWindowConfigurations : IEntityTypeConfiguration<MaintenanceWindowEntity>
{
    public void Configure(EntityTypeBuilder<MaintenanceWindowEntity> builder)
    {
        builder.HasKey(x => x.MaintenanceWindowId);

        builder.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);

        builder.HasMany(x => x.Components).WithOne(x => x.MaintenanceWindow).HasForeignKey(x => x.MaintenanceWindowId);

        builder.HasIndex(x => new { x.ScheduledStart, x.ScheduledEnd });
        builder.HasIndex(x => x.StatusId);

        builder.Property(x => x.Title).HasMaxLength(MaintenanceWindowTitle.MaxLength);
        builder.Property(x => x.Description).HasMaxLength(MaintenanceWindowDescription.MaxLength);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.MaintenanceWindows, Schemas.Service);
    }
}
