using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class NotificationSubscriptionConfigurations : IEntityTypeConfiguration<NotificationSubscriptionEntity>
{
    public void Configure(EntityTypeBuilder<NotificationSubscriptionEntity> builder)
    {
        builder.HasKey(x => x.NotificationSubscriptionId);

        builder.HasOne(x => x.Organization).WithMany(x => x.NotificationSubscriptions).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.Channel).WithMany(x => x.NotificationSubscriptions).HasForeignKey(x => x.ChannelId).IsRequired();
        builder.HasOne(x => x.MinimumSeverity).WithMany().HasForeignKey(x => x.MinimumSeverityId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.OrganizationId, x.IsEnabled, x.IsDeleted });

        builder.Property(x => x.Locale).HasMaxLength(Locale.MaxLength);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.NotificationSubscriptions, Schemas.Service);
    }
}
