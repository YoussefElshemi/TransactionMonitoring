using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApiScopeConfigurations : IEntityTypeConfiguration<ApiScopeEntity>
{
    public void Configure(EntityTypeBuilder<ApiScopeEntity> builder)
    {
        builder.HasKey(x => x.ApiScopeId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.ApiScopes);

        builder.ToTable(TableNames.ApiScopes, Schemas.Identity);
    }
}
