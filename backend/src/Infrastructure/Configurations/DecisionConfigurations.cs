using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DecisionConfigurations : IEntityTypeConfiguration<DecisionEntity>
{
    public void Configure(EntityTypeBuilder<DecisionEntity> builder)
    {
        builder.HasKey(x => x.DecisionId);

        builder.HasOne(x => x.Organization).WithMany(x => x.Decisions).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.Transaction).WithOne(x => x.Decision).HasForeignKey<DecisionEntity>(x => x.TransactionId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ResolvedByUser).WithMany().HasForeignKey(x => x.ResolvedByUserId);
        builder.HasOne(x => x.StatusEntity).WithMany(x => x.Decisions).HasForeignKey(x => x.StatusId).IsRequired();
        builder.HasOne(x => x.ReasonCodeEntity).WithMany(x => x.Decisions).HasForeignKey(x => x.ReasonCodeId).IsRequired();

        builder.HasOne(x => x.HoldExpiryVerdictEntity).WithMany(x => x.Decisions).HasForeignKey(x => x.HoldExpiryVerdictId);

        builder.HasMany(x => x.RuleEvaluations).WithMany(x => x.Decisions).UsingEntity<DecisionRuleEvaluationLinkEntity>();

        builder.HasIndex(x => x.CaseId);
        builder.HasIndex(x => new { x.StatusId, x.ExpiresAt });

        builder.Navigation(x => x.ResolvedByUser).AutoInclude();
        builder.Navigation(x => x.RuleEvaluations).AutoInclude();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Decisions, Schemas.Service);
    }
}
