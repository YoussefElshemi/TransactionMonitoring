using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PartyMappingVersionConfigurations : IEntityTypeConfiguration<PartyMappingVersionEntity>
{
    public void Configure(EntityTypeBuilder<PartyMappingVersionEntity> builder)
    {
        builder.HasKey(x => x.PartyMappingVersionId);

        builder.HasIndex(x => new { x.PartyMappingId, x.VersionNumber }).IsUnique();

        builder.ToTable(TableNames.PartyMappingVersions, Schemas.Service);
    }
}
