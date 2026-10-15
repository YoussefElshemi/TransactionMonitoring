using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RuleEvaluationConfigurations : IEntityTypeConfiguration<RuleEvaluationEntity>
{
    public void Configure(EntityTypeBuilder<RuleEvaluationEntity> builder)
    {
        builder.HasKey(x => x.RuleEvaluationId);

        builder.HasOne(x => x.Organization).WithMany(x => x.RuleEvaluations).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.Rule).WithMany(x => x.RuleEvaluations).HasForeignKey(x => x.RuleId).IsRequired();
        builder.HasOne(x => x.RuleVersion).WithMany().HasForeignKey(x => x.RuleVersionId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Transaction).WithMany(x => x.RuleEvaluations).HasForeignKey(x => x.TransactionId).IsRequired().OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Rule).AutoInclude();
        builder.Navigation(x => x.RuleVersion).AutoInclude();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.RuleEvaluations, Schemas.Service);
    }
}