using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class VelocityBackfillStatusEntityConfiguration : IEntityTypeConfiguration<VelocityBackfillStatusEntity>
{
    public void Configure(EntityTypeBuilder<VelocityBackfillStatusEntity> builder)
    {
        builder.HasKey(x => x.VelocityBackfillStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.VelocityBackfillStatuses);

        builder.ToTable(TableNames.VelocityBackfillStatuses, Schemas.Service);
    }
}
