using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CaseGroupingScopeEntityConfiguration : IEntityTypeConfiguration<CaseGroupingScopeEntity>
{
    public void Configure(EntityTypeBuilder<CaseGroupingScopeEntity> builder)
    {
        builder.HasKey(x => x.CaseGroupingScopeId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.CaseGroupingScopes);

        builder.ToTable(TableNames.CaseGroupingScopes, Schemas.Service);
    }
}
