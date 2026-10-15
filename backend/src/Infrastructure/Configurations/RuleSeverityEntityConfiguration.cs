using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RuleSeverityEntityConfiguration : IEntityTypeConfiguration<RuleSeverityEntity>
{
    public void Configure(EntityTypeBuilder<RuleSeverityEntity> builder)
    {
        builder.HasKey(x => x.RuleSeverityId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.RuleSeverities);

        builder.ToTable(TableNames.RuleSeverities, Schemas.Service);
    }
}
