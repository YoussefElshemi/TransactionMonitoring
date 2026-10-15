using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class OutboxMessageConfigurations : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.HasKey(x => x.OutboxMessageId);

        builder.HasOne(x => x.Channel).WithMany(x => x.OutboxMessages).HasForeignKey(x => x.ChannelId).IsRequired();
        builder.HasOne(x => x.Organization).WithMany(x => x.OutboxMessages).HasForeignKey(x => x.OrganizationId);

        builder.HasOne(x => x.WebhookConfiguration)
            .WithMany(x => x.OutboxMessages)
            .HasForeignKey(x => x.WebhookConfigurationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.OrganizationId, x.ChannelId, x.CreatedAt });

        builder.HasIndex(x => new { x.WebhookConfigurationId, x.CreatedAt });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.OutboxMessages, Schemas.Service);
    }
}