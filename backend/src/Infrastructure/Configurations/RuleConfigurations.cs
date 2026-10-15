using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RuleConfigurations : IEntityTypeConfiguration<RuleEntity>
{
    public void Configure(EntityTypeBuilder<RuleEntity> builder)
    {
        builder.HasKey(x => x.RuleId);

        builder.HasOne(x => x.RuleSet).WithMany(x => x.Rules).HasForeignKey(x => x.RuleSetId).IsRequired();
        builder.HasOne(x => x.Organization).WithMany(x => x.Rules).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.Rules).HasForeignKey(x => x.CreatedByUserId).IsRequired();
        builder.HasMany(x => x.RuleEvaluations).WithOne(x => x.Rule).IsRequired();
        builder.HasOne(x => x.MatchPolarityEntity).WithMany(x => x.Rules).HasForeignKey(x => x.MatchPolarityId).IsRequired();
        builder.HasOne(x => x.ActionEntity).WithMany(x => x.Rules).HasForeignKey(x => x.ActionId).IsRequired();
        builder.HasOne(x => x.SeverityEntity).WithMany(x => x.Rules).HasForeignKey(x => x.SeverityId).IsRequired();
        builder.HasOne(x => x.HoldExpiryVerdictEntity).WithMany(x => x.Rules).HasForeignKey(x => x.HoldExpiryVerdictId);
        builder.HasOne(x => x.AlertSubjectRoleEntity).WithMany(x => x.Rules).HasForeignKey(x => x.AlertSubjectRoleId);

        builder.Navigation(x => x.Organization).AutoInclude();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Rules, Schemas.Service);
    }
}