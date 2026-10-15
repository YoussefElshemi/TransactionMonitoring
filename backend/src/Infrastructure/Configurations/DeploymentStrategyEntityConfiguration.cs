using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DeploymentStrategyEntityConfiguration : IEntityTypeConfiguration<DeploymentStrategyEntity>
{
    public void Configure(EntityTypeBuilder<DeploymentStrategyEntity> builder)
    {
        builder.HasKey(x => x.DeploymentStrategyId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.DeploymentStrategies);

        builder.ToTable(TableNames.DeploymentStrategies, Schemas.Service);
    }
}
