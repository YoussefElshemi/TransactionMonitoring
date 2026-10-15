using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SimulationScenarioEntityConfiguration : IEntityTypeConfiguration<SimulationScenarioEntity>
{
    public void Configure(EntityTypeBuilder<SimulationScenarioEntity> builder)
    {
        builder.HasKey(x => x.SimulationScenarioId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.SimulationScenarios);

        builder.ToTable(TableNames.SimulationScenarios, Schemas.Service);
    }
}
