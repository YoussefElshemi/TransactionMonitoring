using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AuditAnchorConfigurations : IEntityTypeConfiguration<AuditAnchorEntity>
{
    public void Configure(EntityTypeBuilder<AuditAnchorEntity> builder)
    {
        builder.HasKey(x => x.AuditAnchorId);

        builder.Property(x => x.Hash).HasMaxLength(Hash.Length);

        builder.HasIndex(x => new { x.OrganizationId, x.Sequence }).IsUnique();

        builder.ToTable(TableNames.AuditAnchors, Schemas.Service);
    }
}
