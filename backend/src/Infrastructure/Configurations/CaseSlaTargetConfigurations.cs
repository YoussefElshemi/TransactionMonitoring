using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CaseSlaTargetConfigurations : IEntityTypeConfiguration<CaseSlaTargetEntity>
{
    public void Configure(EntityTypeBuilder<CaseSlaTargetEntity> builder)
    {
        builder.HasKey(x => new { x.OrganizationId, x.ModeId, x.SeverityId });

        builder.HasOne(x => x.Organization).WithMany(x => x.CaseSlaTargets).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.SeverityEntity).WithMany(x => x.CaseSlaTargets).HasForeignKey(x => x.SeverityId).IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.CaseSlaTargets, Schemas.Service);
    }
}
