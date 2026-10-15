using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SuspiciousActivityReportStatusEntityConfiguration : IEntityTypeConfiguration<SuspiciousActivityReportStatusEntity>
{
    public void Configure(EntityTypeBuilder<SuspiciousActivityReportStatusEntity> builder)
    {
        builder.HasKey(x => x.SuspiciousActivityReportStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.SuspiciousActivityReportStatuses);

        builder.ToTable(TableNames.SuspiciousActivityReportStatuses, Schemas.Service);
    }
}
