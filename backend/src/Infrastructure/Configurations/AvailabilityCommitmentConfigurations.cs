using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AvailabilityCommitmentConfigurations : IEntityTypeConfiguration<AvailabilityCommitmentEntity>
{
    public void Configure(EntityTypeBuilder<AvailabilityCommitmentEntity> builder)
    {
        builder.HasKey(x => x.AvailabilityCommitmentId);

        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);

        builder.HasMany(x => x.CreditBands)
            .WithOne(x => x.AvailabilityCommitment)
            .HasForeignKey(x => x.AvailabilityCommitmentId);

        builder.HasIndex(x => new { x.OrganizationId, x.EffectiveFrom });

        builder.Property(x => x.AvailabilityTarget).HasPrecision(9, 4);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.AvailabilityCommitments, Schemas.Service);
    }
}
