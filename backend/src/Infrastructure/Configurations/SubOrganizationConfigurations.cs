using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SubOrganizationConfigurations : IEntityTypeConfiguration<SubOrganizationEntity>
{
    public void Configure(EntityTypeBuilder<SubOrganizationEntity> builder)
    {
        builder.HasKey(x => x.SubOrganizationId);

        builder.HasOne(x => x.Organization).WithMany(x => x.SubOrganizations).HasForeignKey(x => x.OrganizationId).IsRequired();

        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.ExternalReference).HasMaxLength(256);

        builder.HasIndex(x => new { x.OrganizationId, x.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.SubOrganizations, Schemas.Service);
    }
}
