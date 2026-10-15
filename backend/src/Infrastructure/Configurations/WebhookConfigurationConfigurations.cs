using Core.Constants;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class WebhookConfigurationConfigurations : IEntityTypeConfiguration<WebhookConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<WebhookConfigurationEntity> builder)
    {
        builder.HasKey(x => x.WebhookConfigurationId);

        builder.HasOne(x => x.Organization).WithMany(x => x.WebhookConfigurations).HasForeignKey(x => x.OrganizationId).IsRequired();

        // Defaulted in the database: pre-versioning rows would otherwise read 0 and WebhookPayloadVersion would throw.
        builder.Property(x => x.PayloadVersion).HasDefaultValue(WebhookPayloadVersions.Current);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.WebhookConfigurations, Schemas.Service);
    }
}