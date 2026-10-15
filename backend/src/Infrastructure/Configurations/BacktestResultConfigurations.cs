using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class BackTestResultConfigurations : IEntityTypeConfiguration<BackTestResultEntity>
{
    public void Configure(EntityTypeBuilder<BackTestResultEntity> builder)
    {
        builder.HasKey(x => x.BackTestResultId);

        builder.HasOne(x => x.BackTest).WithMany(x => x.Results).HasForeignKey(x => x.BackTestId).IsRequired();
        builder.HasOne(x => x.RuleEvaluation).WithMany().HasForeignKey(x => x.RuleEvaluationId).IsRequired();


        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.BackTestResults, Schemas.Service);
    }
}
