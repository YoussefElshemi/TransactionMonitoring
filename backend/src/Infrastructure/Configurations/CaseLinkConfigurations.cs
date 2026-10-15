using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CaseLinkConfigurations : IEntityTypeConfiguration<CaseLinkEntity>
{
    public void Configure(EntityTypeBuilder<CaseLinkEntity> builder)
    {
        builder.HasKey(x => x.CaseLinkId);

        builder.HasOne(x => x.Organization).WithMany(x => x.CaseLinks).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.CaseLinks).HasForeignKey(x => x.CreatedByUserId).IsRequired();

        builder.HasOne(x => x.Case).WithMany(x => x.CaseLinks).HasForeignKey(x => x.CaseId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LinkedCase).WithMany(x => x.LinkedFromCases).HasForeignKey(x => x.LinkedCaseId).IsRequired().OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CaseId, x.IsDeleted });
        builder.HasIndex(x => new { x.LinkedCaseId, x.IsDeleted });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.CaseLinks, Schemas.Service);
    }
}
