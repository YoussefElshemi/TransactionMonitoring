using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DunningStateConfigurations : IEntityTypeConfiguration<DunningStateEntity>
{
    public void Configure(EntityTypeBuilder<DunningStateEntity> builder)
    {
        builder.HasKey(x => x.DunningStateId);

        builder.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).IsRequired();
        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.DunningStage).WithMany().HasForeignKey(x => x.DunningStageId).IsRequired();

        builder.HasIndex(x => x.InvoiceId).IsUnique().HasFilter("\"ResolvedAt\" IS NULL");

        builder.HasIndex(x => new { x.ResolvedAt, x.NextAttemptAt });
        builder.HasIndex(x => x.OrganizationId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.DunningStates, Schemas.Service);
    }
}
