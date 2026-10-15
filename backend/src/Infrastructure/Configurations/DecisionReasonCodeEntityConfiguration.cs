using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DecisionReasonCodeEntityConfiguration : IEntityTypeConfiguration<DecisionReasonCodeEntity>
{
    public void Configure(EntityTypeBuilder<DecisionReasonCodeEntity> builder)
    {
        builder.HasKey(x => x.DecisionReasonCodeId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.DecisionReasonCodes);

        builder.ToTable(TableNames.DecisionReasonCodes, Schemas.Service);
    }
}
