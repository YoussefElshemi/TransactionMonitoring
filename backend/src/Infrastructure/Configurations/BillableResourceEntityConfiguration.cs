using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class BillableResourceEntityConfiguration : IEntityTypeConfiguration<BillableResourceEntity>
{
    public void Configure(EntityTypeBuilder<BillableResourceEntity> builder)
    {
        builder.HasKey(x => x.BillableResourceId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.BillableResources);

        builder.ToTable(TableNames.BillableResources, Schemas.Service);
    }
}
