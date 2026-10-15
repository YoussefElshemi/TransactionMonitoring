using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ServiceComponentEntityConfiguration : IEntityTypeConfiguration<ServiceComponentEntity>
{
    public void Configure(EntityTypeBuilder<ServiceComponentEntity> builder)
    {
        builder.HasKey(x => x.ServiceComponentId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.ServiceComponents);

        builder.ToTable(TableNames.ServiceComponents, Schemas.Service);
    }
}
