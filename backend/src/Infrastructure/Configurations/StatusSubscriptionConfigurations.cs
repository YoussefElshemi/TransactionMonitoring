using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class StatusSubscriptionConfigurations : IEntityTypeConfiguration<StatusSubscriptionEntity>
{
    public void Configure(EntityTypeBuilder<StatusSubscriptionEntity> builder)
    {
        builder.HasKey(x => x.StatusSubscriptionId);

        builder.HasIndex(x => x.NormalizedEmail).IsUnique();

        builder.Property(x => x.ConfirmationTokenHash).HasMaxLength(Hash.Length);
        builder.Property(x => x.UnsubscribeTokenHash).HasMaxLength(Hash.Length);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.StatusSubscriptions, Schemas.Service);
    }
}
