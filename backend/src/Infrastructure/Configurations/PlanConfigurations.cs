using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PlanConfigurations : IEntityTypeConfiguration<PlanEntity>
{
    public void Configure(EntityTypeBuilder<PlanEntity> builder)
    {
        builder.HasKey(x => x.PlanId);

        builder.HasOne(x => x.Organization).WithMany(x => x.Plans).HasForeignKey(x => x.OrganizationId);

        builder.Property(x => x.Name).HasMaxLength(PlanName.MaxLength);
        builder.Property(x => x.Description).HasMaxLength(PlanDescription.MaxLength);

        builder.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Plans, Schemas.Service);
    }
}
