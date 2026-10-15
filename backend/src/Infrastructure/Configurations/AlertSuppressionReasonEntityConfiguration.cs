using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AlertSuppressionReasonEntityConfiguration : IEntityTypeConfiguration<AlertSuppressionReasonEntity>
{
    public void Configure(EntityTypeBuilder<AlertSuppressionReasonEntity> builder)
    {
        builder.HasKey(x => x.AlertSuppressionReasonId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.AlertSuppressionReasons);

        builder.ToTable(TableNames.AlertSuppressionReasons, Schemas.Service);
    }
}
