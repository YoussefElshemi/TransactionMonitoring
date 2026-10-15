using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RuleSetConfigurations : IEntityTypeConfiguration<RuleSetEntity>
{
    public void Configure(EntityTypeBuilder<RuleSetEntity> builder)
    {
        builder.HasKey(x => x.RuleSetId);

        builder.HasMany(x => x.Rules).WithOne(x => x.RuleSet).HasForeignKey(x => x.RuleSetId).IsRequired();
        builder.HasOne(x => x.SubOrganization).WithMany(x => x.RuleSets).HasForeignKey(x => x.SubOrganizationId);

        builder.HasOne(x => x.Organization).WithMany(x => x.RuleSets).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.RuleSets).HasForeignKey(x => x.CreatedByUserId).IsRequired();


        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.RuleSets, Schemas.Service);
    }
}