using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CaseReasonCodeConfigurations : IEntityTypeConfiguration<CaseReasonCodeEntity>
{
    public void Configure(EntityTypeBuilder<CaseReasonCodeEntity> builder)
    {
        builder.HasKey(x => x.CaseReasonCodeId);

        builder.HasOne(x => x.Organization).WithMany(x => x.CaseReasonCodes).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.DispositionEntity).WithMany(x => x.CaseReasonCodes).HasForeignKey(x => x.DispositionId);

        builder.HasIndex(x => new { x.OrganizationId, x.ModeId, x.Code })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.CaseReasonCodes, Schemas.Service);
    }
}
