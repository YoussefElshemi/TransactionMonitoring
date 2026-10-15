using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class IncidentConfigurations : IEntityTypeConfiguration<IncidentEntity>
{
    public void Configure(EntityTypeBuilder<IncidentEntity> builder)
    {
        builder.HasKey(x => x.IncidentId);

        builder.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusId).IsRequired();
        builder.HasOne(x => x.Impact).WithMany().HasForeignKey(x => x.ImpactId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);
        builder.HasMany(x => x.Components).WithOne(x => x.Incident).HasForeignKey(x => x.IncidentId);
        builder.HasMany(x => x.Updates).WithOne(x => x.Incident).HasForeignKey(x => x.IncidentId);

        builder.HasIndex(x => x.IncidentNumber).IsUnique();
        builder.HasIndex(x => new { x.StatusId, x.StartedAt });
        builder.HasIndex(x => x.StartedAt);

        builder.Property(x => x.IncidentNumber).HasMaxLength(IncidentNumber.MaxLength);
        builder.Property(x => x.Title).HasMaxLength(IncidentTitle.MaxLength);
        builder.Property(x => x.Summary).HasMaxLength(IncidentSummary.MaxLength);
        builder.Property(x => x.RootCause).HasMaxLength(RootCause.MaxLength);
        builder.Property(x => x.Remediation).HasMaxLength(Remediation.MaxLength);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Incidents, Schemas.Service);
    }
}
