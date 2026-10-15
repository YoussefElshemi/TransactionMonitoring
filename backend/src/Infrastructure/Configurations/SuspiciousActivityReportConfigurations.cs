using Core.Enums;
using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SuspiciousActivityReportConfigurations : IEntityTypeConfiguration<SuspiciousActivityReportEntity>
{
    public void Configure(EntityTypeBuilder<SuspiciousActivityReportEntity> builder)
    {
        builder.HasKey(x => x.SuspiciousActivityReportId);

        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.StatusEntity).WithMany(x => x.SuspiciousActivityReports).HasForeignKey(x => x.StatusId).IsRequired();
        builder.HasOne(x => x.OpenedByUser).WithMany().HasForeignKey(x => x.OpenedByUserId).IsRequired();
        builder.HasOne(x => x.FiledByUser).WithMany().HasForeignKey(x => x.FiledByUserId);
        builder.HasOne(x => x.DecidedByUser).WithMany().HasForeignKey(x => x.DecidedByUserId);

        builder.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).IsRequired().OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CaseId).IsUnique();

        builder.HasIndex(x => x.FriendlyName).IsUnique();

        builder.HasIndex(x => new { x.OrganizationId, x.StatusId, x.DueAt });

        builder.HasIndex(x => x.DueAt)
            .HasFilter($"\"OverdueNotifiedAt\" IS NULL AND \"StatusId\" NOT IN ({(int)SuspiciousActivityReportStatus.Filed}, {(int)SuspiciousActivityReportStatus.Acknowledged}, {(int)SuspiciousActivityReportStatus.NotFiled})");

        builder.Property(x => x.Narrative).HasMaxLength(Narrative.MaxLength);
        builder.Property(x => x.FilingReference).HasMaxLength(FilingReference.MaxLength);
        builder.Property(x => x.AcknowledgementReference).HasMaxLength(FilingReference.MaxLength);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.SuspiciousActivityReports, Schemas.Service);
    }
}
