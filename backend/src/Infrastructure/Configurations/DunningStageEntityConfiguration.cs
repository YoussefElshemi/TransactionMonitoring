using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DunningStageEntityConfiguration : IEntityTypeConfiguration<DunningStageEntity>
{
    public void Configure(EntityTypeBuilder<DunningStageEntity> builder)
    {
        builder.HasKey(x => x.DunningStageId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.DunningStages);

        builder.ToTable(TableNames.DunningStages, Schemas.Service);
    }
}
