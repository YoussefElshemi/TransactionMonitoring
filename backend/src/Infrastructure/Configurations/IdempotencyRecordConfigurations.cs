using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class IdempotencyRecordConfigurations : IEntityTypeConfiguration<IdempotencyRecordEntity>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecordEntity> builder)
    {
        // Mode is part of the key so a sandbox replay of a live key isn't a cache hit.
        builder.HasKey(x => new { x.OrganizationId, x.ModeId, x.IdempotencyKey });

        builder.HasOne(x => x.Organization).WithMany(x => x.IdempotencyRecords).HasForeignKey(x => x.OrganizationId).IsRequired();

        builder.HasIndex(x => x.ExpiresAt);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.IdempotencyRecords, Schemas.Service);
    }
}
