using Core.Enums;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RuleDeploymentConfigurations : IEntityTypeConfiguration<RuleDeploymentEntity>
{
    public void Configure(EntityTypeBuilder<RuleDeploymentEntity> builder)
    {
        builder.HasKey(x => x.RuleDeploymentId);

        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.Rule).WithMany().HasForeignKey(x => x.RuleId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).IsRequired();
        builder.HasOne(x => x.StrategyEntity).WithMany(x => x.RuleDeployments).HasForeignKey(x => x.StrategyId).IsRequired();
        builder.HasOne(x => x.StatusEntity).WithMany(x => x.RuleDeployments).HasForeignKey(x => x.StatusId).IsRequired();

        builder.HasOne(x => x.TargetRuleVersion)
            .WithMany()
            .HasForeignKey(x => x.TargetRuleVersionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(x => x.PreviousRuleVersion)
            .WithMany()
            .HasForeignKey(x => x.PreviousRuleVersionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(x => x.RuleId)
            .IsUnique()
            .HasFilter($"\"StatusId\" = {(int)RuleDeploymentStatus.InProgress}");


        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.RuleDeployments, Schemas.Service);
    }
}
