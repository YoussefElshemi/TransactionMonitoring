using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class VelocityProfileConfigurations : IEntityTypeConfiguration<VelocityProfileEntity>
{
    public void Configure(EntityTypeBuilder<VelocityProfileEntity> builder)
    {
        builder.HasKey(x => x.VelocityProfileId);

        builder.HasOne(x => x.Organization).WithMany(x => x.VelocityProfiles).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.VelocityProfiles).HasForeignKey(x => x.CreatedByUserId).IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.ModeId }).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.VelocityProfiles, Schemas.Service);
    }
}
