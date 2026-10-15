using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DecisionStatusEntityConfiguration : IEntityTypeConfiguration<DecisionStatusEntity>
{
    public void Configure(EntityTypeBuilder<DecisionStatusEntity> builder)
    {
        builder.HasKey(x => x.DecisionStatusId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.DecisionStatuses);

        builder.ToTable(TableNames.DecisionStatuses, Schemas.Service);
    }
}
