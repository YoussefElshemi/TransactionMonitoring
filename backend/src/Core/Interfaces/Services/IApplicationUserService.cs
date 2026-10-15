using System.Security.Claims;
using Core.Enums;
using Core.Models.Common;
using Core.Models.Users;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IApplicationUserService : IPagedService<ListUserRequest, ApplicationUser>
{
    Task<ApplicationUser> GetAsync(ClaimsPrincipal claimsPrincipal, CancellationToken stoppingToken);
    Task<ApplicationUser> GetAsync(ApplicationUserId userId, CancellationToken stoppingToken);
    Task<ApplicationUser?> FindByEmailAsync(Email email, CancellationToken stoppingToken);
    Task CreateAsync(CreateApplicationUserRequest createApplicationUserRequest, CancellationToken stoppingToken);
    Task<List<ApplicationUser>> ListByIdsAsync(IReadOnlyCollection<ApplicationUserId> userIds, CancellationToken stoppingToken);
    Task SetSubOrganizationAsync(ApplicationUserId userId, SubOrganizationId? subOrganizationId, CancellationToken stoppingToken);

    Task SetDisplayPreferencesAsync(
        ApplicationUserId userId, Patch<Locale?> locale, Patch<DisplayTimeZone?> timeZone, CancellationToken stoppingToken);

    Task AddRoleAsync(ApplicationUserId userId, UserRole userRole, CancellationToken stoppingToken);
    Task RemoveRoleAsync(ApplicationUserId userId, UserRole userRole, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(ApplicationUserId userId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task UpdateAsync(ApplicationUser user, CancellationToken stoppingToken);
    Task DeleteAsync(ApplicationUserId userId, CancellationToken stoppingToken);
    Task RecoverAsync(ApplicationUserId userId, CancellationToken stoppingToken);
}