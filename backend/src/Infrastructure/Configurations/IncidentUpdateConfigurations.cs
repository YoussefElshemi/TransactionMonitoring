using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class IncidentUpdateConfigurations : IEntityTypeConfiguration<IncidentUpdateEntity>
{
    public void Configure(EntityTypeBuilder<IncidentUpdateEntity> builder)
    {
        builder.HasKey(x => x.IncidentUpdateId);

        builder.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusId).IsRequired();
        builder.HasOne(x => x.PostedByUser).WithMany().HasForeignKey(x => x.PostedByUserId);

        builder.HasIndex(x => new { x.IncidentId, x.PostedAt });

        builder.Property(x => x.Body).HasMaxLength(IncidentUpdateBody.MaxLength);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.IncidentUpdates, Schemas.Service);
    }
}
