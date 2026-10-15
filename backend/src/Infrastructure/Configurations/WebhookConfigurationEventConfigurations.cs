using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class WebhookConfigurationEventConfigurations : IEntityTypeConfiguration<WebhookConfigurationEventEntity>
{
    public void Configure(EntityTypeBuilder<WebhookConfigurationEventEntity> builder)
    {
        builder.HasKey(x => new { x.WebhookConfigurationId, x.Event });

        builder.HasOne(x => x.WebhookConfiguration)
            .WithMany(x => x.SubscribedEvents)
            .HasForeignKey(x => x.WebhookConfigurationId)
            .IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.WebhookConfigurationEvents, Schemas.Service);
    }
}
