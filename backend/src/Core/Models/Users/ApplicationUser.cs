using Core.Constants;
using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Organizations;
using Core.ValueObjects;

namespace Core.Models.Users;

public record ApplicationUser : DeletableModel
{
    public required ApplicationUserId UserId { get; init; }
    public required List<UserRole> Roles { get; init; } = null!;
    public required OrganizationId OrganizationId { get; set; }
    public required Organization Organization { get; init; }

    public required SubOrganizationId? SubOrganizationId { get; init; }
    public bool IsEnabled => OrganizationId != SystemConstants.DefaultOrganization;
    public required UserName? UserName { get; init; }
    public required NormalizedUserName? NormalizedUserName { get; init; }
    public required Email? Email { get; init; }
    public required NormalizedEmail? NormalizedEmail { get; init; }
    public required EmailConfirmed EmailConfirmed { get; init; }
    public required PasswordHash? PasswordHash { get; init; }
    public required SecurityStamp? SecurityStamp { get; init; }
    public required ConcurrencyStamp? ConcurrencyStamp { get; init; }
    public required PhoneNumber? PhoneNumber { get; init; }
    public required PhoneNumberConfirmed PhoneNumberConfirmed { get; init; }
    public required TwoFactorEnabled TwoFactorEnabled { get; init; }
    public required LockoutEnd? LockoutEnd { get; init; }
    public required LockoutEnabled LockoutEnabled { get; init; }
    public required AccessFailedCount AccessFailedCount { get; init; }

    public required Locale? Locale { get; init; }
    public required DisplayTimeZone? TimeZone { get; init; }
}