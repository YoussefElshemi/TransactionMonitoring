using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RateLimitOverrideConfigurations : IEntityTypeConfiguration<RateLimitOverrideEntity>
{
    public void Configure(EntityTypeBuilder<RateLimitOverrideEntity> builder)
    {
        builder.HasKey(x => x.RateLimitOverrideId);

        builder.HasOne(x => x.Organization).WithMany(x => x.RateLimitOverrides).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.PoolEntity).WithMany(x => x.RateLimitOverrides).HasForeignKey(x => x.PoolId);

        builder.Property(x => x.Reason).HasMaxLength(RateLimitOverrideReason.MaxLength);

        builder.HasIndex(x => new { x.ExpiresAt, x.IsDeleted });
        builder.HasIndex(x => x.OrganizationId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.RateLimitOverrides, Schemas.Service);
    }
}
