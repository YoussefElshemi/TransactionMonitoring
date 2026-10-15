using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class VelocityBackfillConfigurations : IEntityTypeConfiguration<VelocityBackfillEntity>
{
    public void Configure(EntityTypeBuilder<VelocityBackfillEntity> builder)
    {
        builder.HasKey(x => x.VelocityBackfillId);

        builder.HasOne(x => x.Organization).WithMany(x => x.VelocityBackfills).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.StatusEntity).WithMany(x => x.VelocityBackfills).HasForeignKey(x => x.StatusId).IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.CreatedAt });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.VelocityBackfills, Schemas.Service);
    }
}
