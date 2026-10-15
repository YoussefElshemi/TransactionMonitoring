using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class IncidentStatusEntityConfiguration : IEntityTypeConfiguration<IncidentStatusEntity>
{
    public void Configure(EntityTypeBuilder<IncidentStatusEntity> builder)
    {
        builder.HasKey(x => x.IncidentStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.IncidentStatuses);

        builder.ToTable(TableNames.IncidentStatuses, Schemas.Service);
    }
}
