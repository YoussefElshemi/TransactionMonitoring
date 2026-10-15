using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RateLimitPoolEntityConfiguration : IEntityTypeConfiguration<RateLimitPoolEntity>
{
    public void Configure(EntityTypeBuilder<RateLimitPoolEntity> builder)
    {
        builder.HasKey(x => x.RateLimitPoolId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.RateLimitPools);

        builder.ToTable(TableNames.RateLimitPools, Schemas.Service);
    }
}
