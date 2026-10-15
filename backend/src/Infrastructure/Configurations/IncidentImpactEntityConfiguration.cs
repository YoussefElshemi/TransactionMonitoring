using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class IncidentImpactEntityConfiguration : IEntityTypeConfiguration<IncidentImpactEntity>
{
    public void Configure(EntityTypeBuilder<IncidentImpactEntity> builder)
    {
        builder.HasKey(x => x.IncidentImpactId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.IncidentImpacts);

        builder.ToTable(TableNames.IncidentImpacts, Schemas.Service);
    }
}
