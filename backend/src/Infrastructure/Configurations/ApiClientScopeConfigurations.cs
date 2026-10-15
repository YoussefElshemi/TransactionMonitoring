using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApiClientScopeConfigurations : IEntityTypeConfiguration<ApiClientScopeEntity>
{
    public void Configure(EntityTypeBuilder<ApiClientScopeEntity> builder)
    {
        builder.HasKey(x => new { x.ApiClientId, x.ApiScopeId });

        builder.HasOne(x => x.ApiClient).WithMany(x => x.Scopes).HasForeignKey(x => x.ApiClientId).IsRequired();
        builder.HasOne(x => x.ApiScope).WithMany(x => x.ApiClientScopes).HasForeignKey(x => x.ApiScopeId).IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.ApiClientScopes, Schemas.Identity);
    }
}
