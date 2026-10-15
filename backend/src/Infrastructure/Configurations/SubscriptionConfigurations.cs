using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SubscriptionConfigurations : IEntityTypeConfiguration<SubscriptionEntity>
{
    public void Configure(EntityTypeBuilder<SubscriptionEntity> builder)
    {
        builder.HasKey(x => x.SubscriptionId);

        builder.HasOne(x => x.Organization).WithMany(x => x.Subscriptions).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.PlanVersion).WithMany(x => x.Subscriptions).HasForeignKey(x => x.PlanVersionId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.Subscriptions).HasForeignKey(x => x.CreatedByUserId);

        builder.Navigation(x => x.PlanVersion).AutoInclude();

        builder.HasIndex(x => new { x.OrganizationId, x.EffectiveFrom });

        // No partial unique index on the open stretch: Postgres can't defer it, and cancelling a change closes and reopens rows in one transaction.

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Subscriptions, Schemas.Service);
    }
}
