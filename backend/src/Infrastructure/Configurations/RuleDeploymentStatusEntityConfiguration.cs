using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RuleDeploymentStatusEntityConfiguration : IEntityTypeConfiguration<RuleDeploymentStatusEntity>
{
    public void Configure(EntityTypeBuilder<RuleDeploymentStatusEntity> builder)
    {
        builder.HasKey(x => x.RuleDeploymentStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.RuleDeploymentStatuses);

        builder.ToTable(TableNames.RuleDeploymentStatuses, Schemas.Service);
    }
}
