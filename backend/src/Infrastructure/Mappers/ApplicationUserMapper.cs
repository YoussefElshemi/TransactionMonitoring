using Core.Enums;
using Core.Helpers;
using Core.Models.Users;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class ApplicationUserMapper
{
    public static ApplicationUser ToModel(this ApplicationUserEntity applicationUserEntity)
    {
        return new ApplicationUser
        {
            UserId = new ApplicationUserId(applicationUserEntity.Id),
            Roles = applicationUserEntity.Roles
                .Select(x => RoleHelper.TryMapToUserRole(x.RoleId, out var userRole) ? userRole : (UserRole?)null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList(),
            OrganizationId = new OrganizationId(applicationUserEntity.OrganizationId),
            SubOrganizationId = applicationUserEntity.SubOrganizationId.HasValue ? new SubOrganizationId(applicationUserEntity.SubOrganizationId.Value) : null,
            Organization = applicationUserEntity.Organization.ToModel(),
            UserName = applicationUserEntity.UserName != null ? new UserName(applicationUserEntity.UserName) : null,
            NormalizedUserName = applicationUserEntity.NormalizedUserName != null ? new NormalizedUserName(applicationUserEntity.NormalizedUserName) : null,
            Email = applicationUserEntity.Email != null ? new Email(applicationUserEntity.Email) : null,
            NormalizedEmail = applicationUserEntity.NormalizedEmail != null ? new NormalizedEmail(applicationUserEntity.NormalizedEmail) : null,
            EmailConfirmed = new EmailConfirmed(applicationUserEntity.EmailConfirmed),
            PasswordHash = applicationUserEntity.PasswordHash != null ? new PasswordHash(applicationUserEntity.PasswordHash) : null,
            SecurityStamp = applicationUserEntity.SecurityStamp != null ? new SecurityStamp(applicationUserEntity.SecurityStamp) : null,
            ConcurrencyStamp = applicationUserEntity.ConcurrencyStamp != null ? new ConcurrencyStamp(applicationUserEntity.ConcurrencyStamp) : null,
            PhoneNumber = applicationUserEntity.PhoneNumber != null ? new PhoneNumber(applicationUserEntity.PhoneNumber) : null,
            PhoneNumberConfirmed = new PhoneNumberConfirmed(applicationUserEntity.PhoneNumberConfirmed),
            TwoFactorEnabled = new TwoFactorEnabled(applicationUserEntity.TwoFactorEnabled),
            LockoutEnd = applicationUserEntity.LockoutEnd.HasValue ? new LockoutEnd(applicationUserEntity.LockoutEnd.Value) : null,
            LockoutEnabled = new LockoutEnabled(applicationUserEntity.LockoutEnabled),
            AccessFailedCount = new AccessFailedCount(applicationUserEntity.AccessFailedCount),
            Locale = applicationUserEntity.Locale is not null ? new Locale(applicationUserEntity.Locale) : null,
            TimeZone = applicationUserEntity.TimeZone is not null ? new DisplayTimeZone(applicationUserEntity.TimeZone) : null,
            CreatedAt = new CreatedAt(applicationUserEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(applicationUserEntity.UpdatedAt),
            IsDeleted = new IsDeleted(applicationUserEntity.IsDeleted),
            DeletedAt = applicationUserEntity.DeletedAt.HasValue ? new DeletedAt(applicationUserEntity.DeletedAt.Value) : null
        };
    }

    public static ApplicationUserEntity ToEntity(this ApplicationUser applicationUser)
    {
        return new ApplicationUserEntity
        {
            Id = new ApplicationUserId(applicationUser.UserId),
            OrganizationId = applicationUser.OrganizationId,
            SubOrganizationId = applicationUser.SubOrganizationId.HasValue ? (Guid)applicationUser.SubOrganizationId.Value : null,
            UserName = applicationUser.UserName,
            NormalizedUserName = applicationUser.NormalizedUserName,
            Email = applicationUser.Email,
            NormalizedEmail = applicationUser.NormalizedEmail,
            EmailConfirmed = applicationUser.EmailConfirmed,
            PasswordHash = applicationUser.PasswordHash,
            SecurityStamp = applicationUser.SecurityStamp,
            ConcurrencyStamp = applicationUser.ConcurrencyStamp,
            PhoneNumber = applicationUser.PhoneNumber,
            PhoneNumberConfirmed = applicationUser.PhoneNumberConfirmed,
            TwoFactorEnabled = applicationUser.TwoFactorEnabled,
            LockoutEnd = applicationUser.LockoutEnd,
            LockoutEnabled = applicationUser.LockoutEnabled,
            AccessFailedCount = applicationUser.AccessFailedCount,
            Locale = applicationUser.Locale?.ToString(),
            TimeZone = applicationUser.TimeZone?.ToString(),
            CreatedAt = applicationUser.CreatedAt,
            UpdatedAt = applicationUser.UpdatedAt,
            IsDeleted = applicationUser.IsDeleted,
            DeletedAt = applicationUser.DeletedAt
        };
    }
}