using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class OrganizationStatusEntityConfiguration : IEntityTypeConfiguration<OrganizationStatusEntity>
{
    public void Configure(EntityTypeBuilder<OrganizationStatusEntity> builder)
    {
        builder.HasKey(x => x.OrganizationStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.OrganizationStatuses);

        builder.ToTable(TableNames.OrganizationStatuses, Schemas.Service);
    }
}