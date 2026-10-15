using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class BackTestConfigurations : IEntityTypeConfiguration<BackTestEntity>
{
    public void Configure(EntityTypeBuilder<BackTestEntity> builder)
    {
        builder.HasKey(x => x.BackTestId);

        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.Rule).WithMany().HasForeignKey(x => x.RuleId).IsRequired();
        builder.HasOne(x => x.RuleVersion).WithMany().HasForeignKey(x => x.RuleVersionId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).IsRequired();
        builder.HasOne(x => x.StatusEntity).WithMany(x => x.BackTests).HasForeignKey(x => x.StatusId).IsRequired();


        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.BackTests, Schemas.Service);
    }
}
