using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class IncidentComponentConfigurations : IEntityTypeConfiguration<IncidentComponentEntity>
{
    public void Configure(EntityTypeBuilder<IncidentComponentEntity> builder)
    {
        builder.HasKey(x => new { x.IncidentId, x.ComponentId });

        builder.HasOne(x => x.Component).WithMany().HasForeignKey(x => x.ComponentId).IsRequired();

        builder.HasIndex(x => x.ComponentId);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.IncidentComponents, Schemas.Service);
    }
}
