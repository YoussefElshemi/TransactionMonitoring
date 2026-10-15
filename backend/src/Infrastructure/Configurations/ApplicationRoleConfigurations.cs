using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApplicationRoleConfigurations : IEntityTypeConfiguration<ApplicationRoleEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationRoleEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(256);
        builder.Property(x => x.NormalizedName).HasMaxLength(256);
        builder.Property(x => x.Description).HasMaxLength(512);

        builder.HasOne(x => x.Organization).WithMany(x => x.Roles).HasForeignKey(x => x.OrganizationId);

        builder.HasMany<IdentityUserRole<Guid>>().WithOne().HasForeignKey(x => x.RoleId).IsRequired();
        builder.HasMany<IdentityRoleClaim<Guid>>().WithOne().HasForeignKey(x => x.RoleId).IsRequired();
        builder.HasMany(x => x.RolePermissions).WithOne(x => x.Role).HasForeignKey(x => x.RoleId).IsRequired();
        builder.HasMany(x => x.RoleAssignments).WithOne(x => x.Role).HasForeignKey(x => x.RoleId).IsRequired();

        // Identity's normalized-name index is narrowed to built-in roles; customer roles get their own per-organisation index.
        builder.HasIndex(x => x.NormalizedName)
            .IsUnique()
            .HasFilter("\"OrganizationId\" IS NULL")
            .HasDatabaseName("RoleNameIndex");

        builder.HasIndex(x => new { x.OrganizationId, x.NormalizedName })
            .IsUnique()
            .HasFilter("\"OrganizationId\" IS NOT NULL AND \"IsDeleted\" = false")
            .HasDatabaseName("IX_Roles_OrganizationId_NormalizedName");

        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(SeedData.IdentityRoles);

        builder.ToTable(TableNames.Roles, Schemas.Identity);
    }
}
