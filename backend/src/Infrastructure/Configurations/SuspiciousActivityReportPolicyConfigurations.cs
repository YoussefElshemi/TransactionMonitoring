using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SuspiciousActivityReportPolicyConfigurations : IEntityTypeConfiguration<SuspiciousActivityReportPolicyEntity>
{
    public void Configure(EntityTypeBuilder<SuspiciousActivityReportPolicyEntity> builder)
    {
        builder.HasKey(x => new { x.OrganizationId, x.ModeId });

        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();

        builder.Property(x => x.ReportingEntityId).HasMaxLength(ReportingEntityId.MaxLength);
        builder.Property(x => x.AmountPath).HasMaxLength(PayloadFieldPath.MaxLength);
        builder.Property(x => x.CurrencyPath).HasMaxLength(PayloadFieldPath.MaxLength);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.SuspiciousActivityReportPolicies, Schemas.Service);
    }
}
