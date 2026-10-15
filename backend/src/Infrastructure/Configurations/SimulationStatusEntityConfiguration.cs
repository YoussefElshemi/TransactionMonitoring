using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SimulationStatusEntityConfiguration : IEntityTypeConfiguration<SimulationStatusEntity>
{
    public void Configure(EntityTypeBuilder<SimulationStatusEntity> builder)
    {
        builder.HasKey(x => x.SimulationStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.SimulationStatuses);

        builder.ToTable(TableNames.SimulationStatuses, Schemas.Service);
    }
}
