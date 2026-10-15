using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RuleMatchPolarityEntityConfiguration : IEntityTypeConfiguration<RuleMatchPolarityEntity>
{
    public void Configure(EntityTypeBuilder<RuleMatchPolarityEntity> builder)
    {
        builder.HasKey(x => x.RuleMatchPolarityId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.RuleMatchPolarities);

        builder.ToTable(TableNames.RuleMatchPolarities, Schemas.Service);
    }
}
