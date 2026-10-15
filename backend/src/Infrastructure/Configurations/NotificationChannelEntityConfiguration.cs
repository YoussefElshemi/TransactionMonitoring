using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class NotificationChannelEntityConfiguration : IEntityTypeConfiguration<NotificationChannelEntity>
{
    public void Configure(EntityTypeBuilder<NotificationChannelEntity> builder)
    {
        builder.HasKey(x => x.NotificationChannelId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.NotificationChannels);

        builder.ToTable(TableNames.NotificationChannels, Schemas.Service);
    }
}
