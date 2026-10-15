using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CaseRuleEvaluationLinkConfigurations : IEntityTypeConfiguration<CaseRuleEvaluationLinkEntity>
{
    public void Configure(EntityTypeBuilder<CaseRuleEvaluationLinkEntity> builder)
    {
        builder.HasKey(x => new { x.CaseId, x.RuleEvaluationId });

        builder.HasOne(x => x.Case).WithMany(x => x.CaseRuleEvaluationLinks).HasForeignKey(x => x.CaseId).IsRequired();
        builder.HasOne(x => x.RuleEvaluation).WithMany(x => x.CaseRuleEvaluationLinks).HasForeignKey(x => x.RuleEvaluationId).IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.CaseRuleEvaluationLinks, Schemas.Service);
    }
}