using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class VelocityObservationConfigurations : IEntityTypeConfiguration<VelocityObservationEntity>
{
    public void Configure(EntityTypeBuilder<VelocityObservationEntity> builder)
    {
        builder.HasKey(x => x.VelocityObservationId);

        builder.HasOne(x => x.Organization).WithMany(x => x.VelocityObservations).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.Party).WithMany(x => x.VelocityObservations).HasForeignKey(x => x.PartyId).IsRequired();
        builder.HasOne(x => x.PartyRoleEntity).WithMany(x => x.VelocityObservations).HasForeignKey(x => x.PartyRoleId).IsRequired();
        builder.HasOne(x => x.Transaction).WithMany(x => x.VelocityObservations).HasForeignKey(x => x.TransactionId).IsRequired();

        builder.Property(x => x.Metric).HasMaxLength(VelocityMetricName.MaxLength);
        builder.Property(x => x.Text).HasMaxLength(MetricText.MaxLength);

        builder.HasIndex(x => new { x.TransactionId, x.PartyId, x.PartyRoleId, x.Metric }).IsUnique();

        builder.HasIndex(x => new { x.PartyId, x.Metric, x.ObservedAt });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.VelocityObservations, Schemas.Service);
    }
}
