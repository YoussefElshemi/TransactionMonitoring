using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AlertStatusEntityConfiguration : IEntityTypeConfiguration<AlertStatusEntity>
{
    public void Configure(EntityTypeBuilder<AlertStatusEntity> builder)
    {
        builder.HasKey(x => x.AlertStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.AlertStatuses);

        builder.ToTable(TableNames.AlertStatuses, Schemas.Service);
    }
}
