using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AttachmentConfigurations : IEntityTypeConfiguration<AttachmentEntity>
{
    public void Configure(EntityTypeBuilder<AttachmentEntity> builder)
    {
        builder.HasKey(x => x.AttachmentId);

        builder.HasOne(x => x.Case).WithMany(x => x.Attachments).HasForeignKey(x => x.CaseId).IsRequired();
        builder.HasOne(x => x.Organization).WithMany(x => x.Attachments).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.UploadedByUser).WithMany(x => x.Attachments).HasForeignKey(x => x.UploadedByUserId).IsRequired();
        builder.HasOne(x => x.ScanStatus).WithMany(x => x.Attachments).HasForeignKey(x => x.ScanStatusId).IsRequired();

        builder.HasIndex(x => new { x.CaseId, x.IsDeleted });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Attachments, Schemas.Service);
    }
}
