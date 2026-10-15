using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class HoldExpiryVerdictEntityConfiguration : IEntityTypeConfiguration<HoldExpiryVerdictEntity>
{
    public void Configure(EntityTypeBuilder<HoldExpiryVerdictEntity> builder)
    {
        builder.HasKey(x => x.HoldExpiryVerdictId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.HoldExpiryVerdicts);

        builder.ToTable(TableNames.HoldExpiryVerdicts, Schemas.Service);
    }
}
