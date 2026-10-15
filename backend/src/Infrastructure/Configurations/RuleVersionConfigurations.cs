using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RuleVersionConfigurations : IEntityTypeConfiguration<RuleVersionEntity>
{
    public void Configure(EntityTypeBuilder<RuleVersionEntity> builder)
    {
        builder.HasKey(x => x.RuleVersionId);

        builder.HasOne(x => x.Rule).WithMany(x => x.RuleVersions).HasForeignKey(x => x.RuleId).IsRequired();
        builder.HasOne(x => x.MatchPolarityEntity).WithMany(x => x.RuleVersions).HasForeignKey(x => x.MatchPolarityId).IsRequired();
        builder.HasOne(x => x.ActionEntity).WithMany(x => x.RuleVersions).HasForeignKey(x => x.ActionId).IsRequired();
        builder.HasOne(x => x.SeverityEntity).WithMany(x => x.RuleVersions).HasForeignKey(x => x.SeverityId).IsRequired();

        builder.HasIndex(x => new { x.RuleId, x.VersionNumber }).IsUnique();

        builder.ToTable(TableNames.RuleVersions, Schemas.Service);
    }
}
