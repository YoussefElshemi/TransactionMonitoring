using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SimulationConfigurations : IEntityTypeConfiguration<SimulationEntity>
{
    public void Configure(EntityTypeBuilder<SimulationEntity> builder)
    {
        builder.HasKey(x => x.SimulationId);

        builder.HasOne(x => x.Organization).WithMany(x => x.Simulations).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.ScenarioEntity).WithMany(x => x.Simulations).HasForeignKey(x => x.ScenarioId).IsRequired();
        builder.HasOne(x => x.StatusEntity).WithMany(x => x.Simulations).HasForeignKey(x => x.StatusId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.CreatedAt });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Simulations, Schemas.Service);
    }
}
