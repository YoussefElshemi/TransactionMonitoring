using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class MaintenanceStatusEntityConfiguration : IEntityTypeConfiguration<MaintenanceStatusEntity>
{
    public void Configure(EntityTypeBuilder<MaintenanceStatusEntity> builder)
    {
        builder.HasKey(x => x.MaintenanceStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.MaintenanceStatuses);

        builder.ToTable(TableNames.MaintenanceStatuses, Schemas.Service);
    }
}
