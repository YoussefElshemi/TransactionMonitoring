using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class EscalationRuleConfigurations : IEntityTypeConfiguration<EscalationRuleEntity>
{
    public void Configure(EntityTypeBuilder<EscalationRuleEntity> builder)
    {
        builder.HasKey(x => x.EscalationRuleId);

        builder.HasOne(x => x.Organization).WithMany(x => x.EscalationRules).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.MinimumSeverityEntity).WithMany(x => x.EscalationRuleMinimums).HasForeignKey(x => x.MinimumSeverityId);
        builder.HasOne(x => x.RaiseSeverityToEntity).WithMany(x => x.EscalationRuleRaises).HasForeignKey(x => x.RaiseSeverityToId);

        builder.HasOne(x => x.TargetQueue).WithMany(x => x.EscalationRules).HasForeignKey(x => x.TargetQueueId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.OrganizationId, x.ModeId, x.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.OrganizationId, x.IsEnabled, x.Priority });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.EscalationRules, Schemas.Service);
    }
}
