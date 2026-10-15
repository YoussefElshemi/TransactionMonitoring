using System.Security.Claims;
using Core.Enums;
using Core.Models.Common;
using Core.Models.Users;
using Core.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace Core.Interfaces.Repositories;

public interface IApplicationUserRepository : IPagedRepository<ListUserRequest, ApplicationUser>
{
    Task<ApplicationUser> GetByIdAsync(ApplicationUserId userId, CancellationToken stoppingToken);
    Task<List<ApplicationUser>> ListByIdsAsync(IReadOnlyCollection<ApplicationUserId> userIds, CancellationToken stoppingToken);
    Task<ApplicationUser> GetAsync(ClaimsPrincipal claimsPrincipal, CancellationToken stoppingToken);
    Task<ApplicationUser?> FindByEmailAsync(Email email, CancellationToken stoppingToken);
    Task<IdentityResult> CreateAsync(CreateApplicationUserRequest createApplicationUserRequest, CancellationToken stoppingToken);
    Task<IdentityResult> SetSubOrganizationAsync(ApplicationUserId userId, SubOrganizationId? subOrganizationId, CancellationToken stoppingToken);

    Task<IdentityResult> SetDisplayPreferencesAsync(
        ApplicationUserId userId, Patch<Locale?> locale, Patch<DisplayTimeZone?> timeZone, CancellationToken stoppingToken);

    Task AddRoleAsync(ApplicationUserId userId, UserRole userRole, CancellationToken stoppingToken);
    Task RemoveRoleAsync(ApplicationUserId userId, UserRole userRole, CancellationToken stoppingToken);
    Task<Exists> ExistsByIdAsync(ApplicationUserId userId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(ClaimsPrincipal claimsPrincipal, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken stoppingToken);
    Task<IdentityResult> DeleteAsync(ApplicationUserId userId, CancellationToken stoppingToken);
    Task<IdentityResult> RecoverAsync(ApplicationUserId userId, CancellationToken stoppingToken);
}