using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AlertConfigurations : IEntityTypeConfiguration<AlertEntity>
{
    public void Configure(EntityTypeBuilder<AlertEntity> builder)
    {
        builder.HasKey(x => x.AlertId);

        builder.HasOne(x => x.Organization).WithMany(x => x.Alerts).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.StatusEntity).WithMany(x => x.Alerts).HasForeignKey(x => x.StatusId).IsRequired();
        builder.HasOne(x => x.SeverityEntity).WithMany(x => x.Alerts).HasForeignKey(x => x.SeverityId).IsRequired();
        builder.HasOne(x => x.Rule).WithMany(x => x.Alerts).HasForeignKey(x => x.RuleId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RuleVersion).WithMany(x => x.Alerts).HasForeignKey(x => x.RuleVersionId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RuleEvaluation).WithMany(x => x.Alerts).HasForeignKey(x => x.RuleEvaluationId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Transaction).WithMany(x => x.Alerts).HasForeignKey(x => x.TransactionId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Party).WithMany(x => x.Alerts).HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PartyRoleEntity).WithMany(x => x.Alerts).HasForeignKey(x => x.PartyRoleId);
        builder.HasOne(x => x.Case).WithMany(x => x.Alerts).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SuppressionReasonEntity).WithMany(x => x.Alerts).HasForeignKey(x => x.SuppressionReasonId);

        builder.HasIndex(x => x.RuleEvaluationId).IsUnique();

        builder.HasIndex(x => new { x.OrganizationId, x.PartyId, x.RuleId, x.RaisedAt });

        builder.HasIndex(x => new { x.OrganizationId, x.PartyId, x.StatusId, x.RaisedAt });

        builder.HasIndex(x => x.CaseId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Alerts, Schemas.Service);
    }
}
