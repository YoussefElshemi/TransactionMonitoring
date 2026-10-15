using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class QueueRoutingPolicyEntityConfiguration : IEntityTypeConfiguration<QueueRoutingPolicyEntity>
{
    public void Configure(EntityTypeBuilder<QueueRoutingPolicyEntity> builder)
    {
        builder.HasKey(x => x.QueueRoutingPolicyId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.QueueRoutingPolicies);

        builder.ToTable(TableNames.QueueRoutingPolicies, Schemas.Service);
    }
}
