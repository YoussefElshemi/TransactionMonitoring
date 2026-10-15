using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class InvitationConfigurations : IEntityTypeConfiguration<InvitationEntity>
{
    public void Configure(EntityTypeBuilder<InvitationEntity> builder)
    {
        builder.HasKey(x => x.InvitationId);

        builder.HasOne(x => x.Organization).WithMany(x => x.Invitations).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        builder.HasOne(x => x.SubOrganization).WithMany().HasForeignKey(x => x.SubOrganizationId);

        builder.HasOne(x => x.InvitedByUser).WithMany().HasForeignKey(x => x.InvitedByUserId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        builder.HasOne(x => x.AcceptedByUser).WithMany().HasForeignKey(x => x.AcceptedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RevokedByUser).WithMany().HasForeignKey(x => x.RevokedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.TokenHash).HasMaxLength(Hash.Length);

        builder.HasIndex(x => x.TokenHash).IsUnique();

        builder.HasIndex(x => new { x.OrganizationId, x.NormalizedEmail });

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.ToTable(TableNames.Invitations, Schemas.Identity);
    }
}
