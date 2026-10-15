using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PlanVersionConfigurations : IEntityTypeConfiguration<PlanVersionEntity>
{
    public void Configure(EntityTypeBuilder<PlanVersionEntity> builder)
    {
        builder.HasKey(x => x.PlanVersionId);

        builder.HasOne(x => x.Plan).WithMany(x => x.PlanVersions).HasForeignKey(x => x.PlanId).IsRequired();
        builder.HasOne(x => x.PublishedByUser).WithMany(x => x.PlanVersions).HasForeignKey(x => x.PublishedByUserId);
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).IsRequired();

        builder.Navigation(x => x.Plan).AutoInclude();

        builder.Navigation(x => x.Prices).AutoInclude();

        builder.HasIndex(x => new { x.PlanId, x.VersionNumber }).IsUnique();

        builder.ToTable(TableNames.PlanVersions, Schemas.Service);
    }
}
