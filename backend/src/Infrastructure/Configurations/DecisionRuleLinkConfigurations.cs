using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DecisionRuleEvaluationLinkConfigurations : IEntityTypeConfiguration<DecisionRuleEvaluationLinkEntity>
{
    public void Configure(EntityTypeBuilder<DecisionRuleEvaluationLinkEntity> builder)
    {
        builder.HasKey(x => new { x.DecisionId, x.RuleEvaluationId });

        builder.HasOne(x => x.Decision).WithMany(x => x.DecisionRuleEvaluationLinks).HasForeignKey(x => x.DecisionId).IsRequired();
        builder.HasOne(x => x.RuleEvaluation).WithMany(x => x.DecisionRuleEvaluationLinks).HasForeignKey(x => x.RuleEvaluationId).IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.DecisionRuleEvaluationLinks, Schemas.Service);
    }
}
