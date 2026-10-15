using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class QueueRuleConfigurations : IEntityTypeConfiguration<QueueRuleEntity>
{
    public void Configure(EntityTypeBuilder<QueueRuleEntity> builder)
    {
        builder.HasKey(x => new { x.QueueId, x.RuleId });

        builder.HasOne(x => x.Rule).WithMany(x => x.QueueRules).HasForeignKey(x => x.RuleId).OnDelete(DeleteBehavior.Restrict).IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.QueueRules, Schemas.Service);
    }
}
