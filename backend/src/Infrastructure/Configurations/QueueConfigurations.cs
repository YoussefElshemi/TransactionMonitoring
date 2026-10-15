using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class QueueConfigurations : IEntityTypeConfiguration<QueueEntity>
{
    public void Configure(EntityTypeBuilder<QueueEntity> builder)
    {
        builder.HasKey(x => x.QueueId);

        builder.HasOne(x => x.Organization).WithMany(x => x.Queues).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.SubOrganization).WithMany(x => x.Queues).HasForeignKey(x => x.SubOrganizationId);
        builder.HasOne(x => x.RoutingPolicyEntity).WithMany(x => x.Queues).HasForeignKey(x => x.RoutingPolicyId).IsRequired();
        builder.HasOne(x => x.MinimumSeverityEntity).WithMany(x => x.Queues).HasForeignKey(x => x.MinimumSeverityId);
        builder.HasMany(x => x.QueueRules).WithOne(x => x.Queue).HasForeignKey(x => x.QueueId).IsRequired();
        builder.HasMany(x => x.Members).WithOne(x => x.Queue).HasForeignKey(x => x.QueueId).IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.ModeId, x.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.OrganizationId, x.IsEnabled, x.Priority });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Queues, Schemas.Service);
    }
}
