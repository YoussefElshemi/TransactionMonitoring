using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApprovalPolicyConfigurations : IEntityTypeConfiguration<ApprovalPolicyEntity>
{
    public void Configure(EntityTypeBuilder<ApprovalPolicyEntity> builder)
    {
        builder.HasKey(x => new { x.OrganizationId, x.ModeId });

        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.CaseDecisionMinimumSeverityEntity).WithMany().HasForeignKey(x => x.CaseDecisionMinimumSeverityId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.ApprovalPolicies, Schemas.Service);
    }
}
