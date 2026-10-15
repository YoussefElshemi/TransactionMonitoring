using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApplicationUserConfigurations : IEntityTypeConfiguration<ApplicationUserEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationUserEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
        builder.HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex");

        builder.Property(x => x.UserName).HasMaxLength(256);
        builder.Property(x => x.NormalizedUserName).HasMaxLength(256);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.NormalizedEmail).HasMaxLength(256);

        builder.HasMany<IdentityUserClaim<Guid>>().WithOne().HasForeignKey(x => x.UserId).IsRequired();
        builder.HasMany<IdentityUserLogin<Guid>>().WithOne().HasForeignKey(x => x.UserId).IsRequired();
        builder.HasMany<IdentityUserToken<Guid>>().WithOne().HasForeignKey(x => x.UserId).IsRequired();
        builder.HasMany<IdentityUserRole<Guid>>().WithOne().HasForeignKey(x => x.UserId).IsRequired();
        builder.HasOne(x => x.Organization).WithMany(x => x.ApplicationUsers).HasForeignKey(x => x.OrganizationId).IsRequired();
        builder.HasOne(x => x.SubOrganization).WithMany(x => x.ApplicationUsers).HasForeignKey(x => x.SubOrganizationId);
        builder.HasMany(x => x.RoleAssignments).WithOne(x => x.User).HasForeignKey(x => x.UserId).IsRequired();
        builder.HasMany(x => x.ApiClients).WithOne(x => x.CreatedByUser).HasForeignKey(x => x.CreatedByUserId).IsRequired();
        builder.HasMany(x => x.Invoices).WithOne(x => x.CreatedByUser).HasForeignKey(x => x.CreatedByUserId);
        builder.HasMany(x => x.Rules).WithOne(x => x.CreatedByUser).HasForeignKey(x => x.CreatedByUserId).IsRequired();
        builder.HasMany(x => x.Cases).WithOne(x => x.AssignedToUser).HasForeignKey(x => x.AssignedToUserId);
        builder.HasMany(x => x.Roles).WithOne().HasForeignKey(x => x.UserId);

        builder.Navigation(x => x.Organization).AutoInclude();
        builder.Navigation(x => x.Roles).AutoInclude();

        builder.Property(x => x.Locale).HasMaxLength(Locale.MaxLength);
        builder.Property(x => x.TimeZone).HasMaxLength(DisplayTimeZone.MaxLength);

        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();

        builder.ToTable(TableNames.Users, Schemas.Identity);
    }
}