using System.Security.Claims;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Users;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ApplicationUserRepository(
    UserManager<ApplicationUserEntity> userManager,
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListUserRequest, ApplicationUserEntity, ApplicationUser>, IApplicationUserRepository
{
    public async Task<ApplicationUser> GetByIdAsync(ApplicationUserId userId, CancellationToken stoppingToken)
    {
        var user = await userManager.Users
            .FirstAsync(x => x.Id == userId && !x.IsDeleted, stoppingToken);

        return user.ToModel();
    }

    public async Task<List<ApplicationUser>> ListByIdsAsync(IReadOnlyCollection<ApplicationUserId> userIds, CancellationToken stoppingToken)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        var userIdValues = userIds.Select(x => (Guid)x).Distinct().ToList();

        var users = await userManager.Users
            .Where(x => userIdValues.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        return users.ConvertAll(x => x.ToModel());
    }

    public Task<ApplicationUser> GetAsync(ClaimsPrincipal claimsPrincipal, CancellationToken stoppingToken)
    {
        var id = GetApplicationUserId(claimsPrincipal);
        return GetByIdAsync(id, stoppingToken);
    }

    public async Task<ApplicationUser?> FindByEmailAsync(Email email, CancellationToken stoppingToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        return user is null || user.IsDeleted ? null : user.ToModel();
    }

    public async Task<IdentityResult> CreateAsync(CreateApplicationUserRequest createApplicationUserRequest, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var userEntity = new ApplicationUserEntity
        {
            Id = createApplicationUserRequest.UserId,
            OrganizationId = createApplicationUserRequest.OrganizationId,
            SubOrganizationId = createApplicationUserRequest.SubOrganizationId.HasValue
                ? (Guid)createApplicationUserRequest.SubOrganizationId.Value
                : null,
            UserName = createApplicationUserRequest.UserName,
            Email = createApplicationUserRequest.Email,
            EmailConfirmed = createApplicationUserRequest.EmailConfirmed,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            DeletedAt = null
        };

        var result = await userManager.CreateAsync(userEntity, createApplicationUserRequest.Password);

        // UserManager leaves the insert tracked; detach so the next no-tracking read of this user can attach.
        context.Entry(userEntity).State = EntityState.Detached;

        return result;
    }

    public async Task AddRoleAsync(ApplicationUserId userId, UserRole userRole, CancellationToken stoppingToken)
    {
        var user = await userManager.Users
            .FirstAsync(x => x.Id == userId && !x.IsDeleted, stoppingToken);

        await userManager.AddToRoleAsync(user, userRole.ToString());
    }

    public async Task RemoveRoleAsync(ApplicationUserId userId, UserRole userRole, CancellationToken stoppingToken)
    {
        var userEntity = await userManager.Users
            .FirstAsync(x => x.Id == userId && !x.IsDeleted, stoppingToken);

        await RemoveRoleAsync(userEntity, userRole, stoppingToken);
    }

    public async Task<Exists> ExistsByIdAsync(ApplicationUserId userId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await userManager.Users
            .AnyAsync(x => x.Id == userId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public Task<Exists> ExistsAsync(ClaimsPrincipal claimsPrincipal, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var id = GetApplicationUserId(claimsPrincipal);
        return ExistsByIdAsync(id, includeDeleted, stoppingToken);
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken stoppingToken)
    {
        var userEntity = await userManager.Users
            .FirstAsync(x => x.Id == user.UserId && !x.IsDeleted, stoppingToken);

        var organizationChanged = userEntity.OrganizationId != user.OrganizationId;
        userEntity.OrganizationId = user.OrganizationId;
        context.Entry(userEntity).Property(e => e.OrganizationId).IsModified = true;

        var result = await userManager.UpdateAsync(userEntity);

        if (result.Succeeded && organizationChanged)
        {
            await RemoveRoleAsync(userEntity, UserRole.Manager, stoppingToken);
        }

        return result;
    }

    public async Task<IdentityResult> SetSubOrganizationAsync(ApplicationUserId userId, SubOrganizationId? subOrganizationId, CancellationToken stoppingToken)
    {
        var userEntity = await userManager.Users
            .FirstAsync(x => x.Id == userId && !x.IsDeleted, stoppingToken);

        userEntity.SubOrganizationId = subOrganizationId.HasValue ? (Guid)subOrganizationId.Value : null;
        context.Entry(userEntity).Property(e => e.SubOrganizationId).IsModified = true;

        return await userManager.UpdateAsync(userEntity);
    }

    // Per-property Modified: a plain Update on this detached entity would write back the security stamp and password hash.
    public async Task<IdentityResult> SetDisplayPreferencesAsync(
        ApplicationUserId userId, Patch<Locale?> locale, Patch<DisplayTimeZone?> timeZone, CancellationToken stoppingToken)
    {
        var userEntity = await userManager.Users
            .FirstAsync(x => x.Id == userId && !x.IsDeleted, stoppingToken);

        if (locale.IsSet)
        {
            userEntity.Locale = locale.Value?.ToString();
            context.Entry(userEntity).Property(e => e.Locale).IsModified = true;
        }

        if (timeZone.IsSet)
        {
            userEntity.TimeZone = timeZone.Value?.ToString();
            context.Entry(userEntity).Property(e => e.TimeZone).IsModified = true;
        }

        return await userManager.UpdateAsync(userEntity);
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationUserId userId, CancellationToken stoppingToken)
    {
        var userEntity = await userManager.Users
            .FirstAsync(x => x.Id == userId && !x.IsDeleted, stoppingToken);

        userEntity.IsDeleted = true;
        userEntity.DeletedAt = timeProvider.GetUtcNow();

        var result = await userManager.UpdateAsync(userEntity);

        return result;
    }

    public async Task<IdentityResult> RecoverAsync(ApplicationUserId userId, CancellationToken stoppingToken)
    {
        var userEntity = await userManager.Users
            .FirstAsync(x => x.Id == userId, stoppingToken);

        userEntity.IsDeleted = false;
        userEntity.DeletedAt = null;

        var result = await userManager.UpdateAsync(userEntity);

        return result;
    }

    protected override Task<List<ApplicationUser>> MapPageAsync(List<ApplicationUserEntity> items, ListUserRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<ApplicationUserEntity> OrderByExpression()
    {
        return new OrderByExpression<ApplicationUserEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<ApplicationUserEntity> FilterByPagedRequest(ListUserRequest listUserRequest)
    {
        var queryable = userManager.Users
            .Where(x => !x.IsDeleted);

        if (listUserRequest.UserName.HasValue)
        {
            queryable = queryable
                .Where(x => x.UserName != null && EF.Functions.ILike(x.UserName, $"%{listUserRequest.UserName}%"));
        }

        return queryable;
    }

    protected override IQueryable<ApplicationUserEntity> FilterByDateRange(IQueryable<ApplicationUserEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }

    private async Task RemoveRoleAsync(ApplicationUserEntity userEntity, UserRole userRole, CancellationToken stoppingToken)
    {
        var roleId = RoleHelper.MapFromUserRole(userRole);
        var userRoleEntity = userEntity.Roles.FirstOrDefault(x => x.RoleId == roleId);

        if (userRoleEntity is null)
        {
            return;
        }

        context.Attach(userEntity);
        context.Set<IdentityUserRole<Guid>>().Remove(userRoleEntity);

        await context.SaveChangesAsync(stoppingToken);
    }

    private static ApplicationUserId GetApplicationUserId(ClaimsPrincipal claimsPrincipal)
    {
        return new ApplicationUserId(Guid.Parse(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)!));
    }
}