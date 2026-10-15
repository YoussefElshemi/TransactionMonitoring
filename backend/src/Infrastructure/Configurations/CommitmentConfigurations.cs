using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CommitmentConfigurations : IEntityTypeConfiguration<CommitmentEntity>
{
    public void Configure(EntityTypeBuilder<CommitmentEntity> builder)
    {
        builder.HasKey(x => x.CommitmentId);

        builder.HasOne(x => x.Organization).WithMany(x => x.Commitments).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany(x => x.Commitments).HasForeignKey(x => x.CreatedByUserId);

        builder.HasIndex(x => new { x.OrganizationId, x.EffectiveFrom });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Commitments, Schemas.Service);
    }
}
