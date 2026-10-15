using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AttachmentScanStatusEntityConfiguration : IEntityTypeConfiguration<AttachmentScanStatusEntity>
{
    public void Configure(EntityTypeBuilder<AttachmentScanStatusEntity> builder)
    {
        builder.HasKey(x => x.AttachmentScanStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.AttachmentScanStatuses);

        builder.ToTable(TableNames.AttachmentScanStatuses, Schemas.Service);
    }
}
