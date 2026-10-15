using Core.Enums;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApprovalConfigurations : IEntityTypeConfiguration<ApprovalEntity>
{
    public void Configure(EntityTypeBuilder<ApprovalEntity> builder)
    {
        builder.HasKey(x => x.ApprovalId);

        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.SubjectEntity).WithMany(x => x.Approvals).HasForeignKey(x => x.SubjectId).IsRequired();
        builder.HasOne(x => x.StatusEntity).WithMany(x => x.Approvals).HasForeignKey(x => x.StatusId).IsRequired();
        builder.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).IsRequired();
        builder.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId);
        builder.HasOne(x => x.ProposedStatusEntity).WithMany().HasForeignKey(x => x.ProposedStatusId);
        builder.HasOne(x => x.ProposedDispositionEntity).WithMany().HasForeignKey(x => x.ProposedDispositionId);

        builder.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RuleDeployment).WithMany().HasForeignKey(x => x.RuleDeploymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProposedCaseReasonCode).WithMany().HasForeignKey(x => x.ProposedCaseReasonCodeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SuspiciousActivityReport).WithMany().HasForeignKey(x => x.SuspiciousActivityReportId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CaseId)
            .IsUnique()
            .HasFilter($"\"StatusId\" = {(int)ApprovalStatus.Pending} AND \"CaseId\" IS NOT NULL");

        builder.HasIndex(x => x.RuleDeploymentId)
            .IsUnique()
            .HasFilter($"\"StatusId\" = {(int)ApprovalStatus.Pending} AND \"RuleDeploymentId\" IS NOT NULL");

        builder.HasIndex(x => x.SuspiciousActivityReportId)
            .IsUnique()
            .HasFilter($"\"StatusId\" = {(int)ApprovalStatus.Pending} AND \"SuspiciousActivityReportId\" IS NOT NULL");

        builder.HasIndex(x => new { x.OrganizationId, x.StatusId, x.CreatedAt });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Approvals, Schemas.Service);
    }
}
