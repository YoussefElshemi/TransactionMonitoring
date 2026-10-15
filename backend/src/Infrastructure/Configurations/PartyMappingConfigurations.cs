using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PartyMappingConfigurations : IEntityTypeConfiguration<PartyMappingEntity>
{
    public void Configure(EntityTypeBuilder<PartyMappingEntity> builder)
    {
        builder.HasKey(x => x.PartyMappingId);

        builder.HasOne(x => x.Organization).WithMany(x => x.PartyMappings).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.PartyMappings).HasForeignKey(x => x.CreatedByUserId).IsRequired();
        builder.HasMany(x => x.Versions).WithOne(x => x.PartyMapping).HasForeignKey(x => x.PartyMappingId).IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.ModeId }).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.PartyMappings, Schemas.Service);
    }
}
