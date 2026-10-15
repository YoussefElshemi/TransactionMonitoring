using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApiClientSecretConfigurations : IEntityTypeConfiguration<ApiClientSecretEntity>
{
    public void Configure(EntityTypeBuilder<ApiClientSecretEntity> builder)
    {
        builder.HasKey(x => x.ApiClientSecretId);

        builder.HasIndex(x => x.ApiClientId);

        builder.HasOne(x => x.ApiClient).WithMany(x => x.Secrets).HasForeignKey(x => x.ApiClientId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.ApiClientSecrets).HasForeignKey(x => x.CreatedByUserId).IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.ApiClientSecrets, Schemas.Identity);
    }
}
