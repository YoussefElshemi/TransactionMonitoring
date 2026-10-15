using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RuleActionEntityConfiguration : IEntityTypeConfiguration<RuleActionEntity>
{
    public void Configure(EntityTypeBuilder<RuleActionEntity> builder)
    {
        builder.HasKey(x => x.RuleActionId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.RuleActions);

        builder.ToTable(TableNames.RuleActions, Schemas.Service);
    }
}
