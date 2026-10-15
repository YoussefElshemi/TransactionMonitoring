using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApiClientConfigurations : IEntityTypeConfiguration<ApiClientEntity>
{
    public void Configure(EntityTypeBuilder<ApiClientEntity> builder)
    {
        builder.HasKey(x => x.ApiClientId);

        builder.HasIndex(x => x.ClientId).IsUnique();

        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.ApiClients).HasForeignKey(x => x.CreatedByUserId).IsRequired();
        builder.HasOne(x => x.Organization).WithMany(x => x.ApiClients).HasForeignKey(x => x.OrganizationId).IsRequired();

        builder.Navigation(x => x.Organization).AutoInclude();

        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Property(x => x.ApiVersion).HasMaxLength(10);

        builder.ToTable(TableNames.ApiClients, Schemas.Identity);
    }
}
