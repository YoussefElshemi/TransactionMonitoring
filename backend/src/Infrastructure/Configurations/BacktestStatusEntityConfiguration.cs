using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class BackTestStatusEntityConfiguration : IEntityTypeConfiguration<BackTestStatusEntity>
{
    public void Configure(EntityTypeBuilder<BackTestStatusEntity> builder)
    {
        builder.HasKey(x => x.BackTestStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.BackTestStatuses);

        builder.ToTable(TableNames.BackTestStatuses, Schemas.Service);
    }
}
