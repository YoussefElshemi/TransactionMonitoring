using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Roles;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RoleRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider)
    : PagedRepository<ListRoleRequest, ApplicationRoleEntity, Role>, IRoleRepository
{
    private const string LikeEscapeCharacter = "\\";

    public async Task AddAsync(Role role, CancellationToken stoppingToken)
    {
        await context.Roles.AddAsync(role.ToEntity(), stoppingToken);
    }

    public async Task<Role> GetAsync(RoleId roleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var roleEntity = await Detail()
            .FirstAsync(x => x.Id == roleId && (includeDeleted || !x.IsDeleted), stoppingToken);

        return roleEntity.ToModel();
    }

    // Escaped: this is an equality test written as LIKE, and underscores would otherwise be wildcards.
    public async Task<Role?> FindByNameAsync(OrganizationId organizationId, RoleName name, CancellationToken stoppingToken)
    {
        var value = EscapeLikePattern(name.ToString());

        var roleEntity = await Detail()
            .FirstOrDefaultAsync(x => x.OrganizationId == (Guid)organizationId &&
                                      EF.Functions.ILike(x.Name!, value, LikeEscapeCharacter) &&
                                      !x.IsDeleted, stoppingToken);

        return roleEntity?.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, RoleId roleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.Roles
            .AnyAsync(x => x.Id == roleId &&
                           (x.IsBuiltIn || x.OrganizationId == (Guid)organizationId) &&
                           (includeDeleted || !x.IsDeleted), stoppingToken);

        return new Exists(exists);
    }

    public async Task<RoleUsage> GetUsageAsync(RoleId roleId, CancellationToken stoppingToken)
    {
        var roleEntity = await context.Roles
            .FirstAsync(x => x.Id == roleId, stoppingToken);

        var assignments = await context.RoleAssignments
            .Where(x => x.RoleId == roleId)
            .Select(x => x.UserId)
            .ToListAsync(stoppingToken);

        return new RoleUsage
        {
            RoleId = new RoleId(roleEntity.Id),
            Name = new RoleName(roleEntity.Name!),
            AssignmentCount = new TotalCount(assignments.Count),
            UserIds = assignments.Distinct().Select(x => new ApplicationUserId(x)).ToList()
        };
    }

    public async Task SetPermissionsAsync(RoleId roleId, IReadOnlyCollection<Permission> permissions, CancellationToken stoppingToken)
    {
        var existing = await context.RolePermissions
            .Where(x => x.RoleId == roleId)
            .ToListAsync(stoppingToken);

        context.RolePermissions.RemoveRange(existing);

        var now = timeProvider.GetUtcNow();

        await context.RolePermissions.AddRangeAsync(permissions.Distinct().Select(x => new RolePermissionEntity
        {
            RoleId = roleId,
            PermissionId = (int)x,
            CreatedAt = now,
            UpdatedAt = now
        }), stoppingToken);
    }

    public async Task UpdateAsync(UpdateRoleRequest updateRoleRequest, CancellationToken stoppingToken)
    {
        var roleEntity = await context.Roles
            .FirstAsync(x => x.Id == updateRoleRequest.RoleId && !x.IsDeleted, stoppingToken);

        if (updateRoleRequest.Name.HasValue)
        {
            roleEntity.Name = updateRoleRequest.Name.Value;
            roleEntity.NormalizedName = updateRoleRequest.Name.Value.ToString().ToUpperInvariant();
        }

        roleEntity.Description = updateRoleRequest.Description?.ToString() ?? roleEntity.Description;
        roleEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Roles.Update(roleEntity);
    }

    public async Task DeleteAsync(RoleId roleId, CancellationToken stoppingToken)
    {
        var roleEntity = await context.Roles
            .FirstAsync(x => x.Id == roleId, stoppingToken);

        roleEntity.IsDeleted = true;
        roleEntity.DeletedAt = timeProvider.GetUtcNow();
        roleEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Roles.Update(roleEntity);
    }

    public async Task RecoverAsync(RoleId roleId, CancellationToken stoppingToken)
    {
        var roleEntity = await context.Roles
            .FirstAsync(x => x.Id == roleId, stoppingToken);

        roleEntity.IsDeleted = false;
        roleEntity.DeletedAt = null;
        roleEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Roles.Update(roleEntity);
    }

    protected override Task<List<Role>> MapPageAsync(List<ApplicationRoleEntity> items, ListRoleRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<ApplicationRoleEntity> OrderByExpression()
    {
        return new OrderByExpression<ApplicationRoleEntity, DateTimeOffset>(x => x.CreatedAt, true, x => x.CreatedAt);
    }

    protected override IQueryable<ApplicationRoleEntity> FilterByPagedRequest(ListRoleRequest listRoleRequest)
    {
        var queryable = Detail()
            .Where(x => (x.IsBuiltIn || x.OrganizationId == (Guid)listRoleRequest.OrganizationId) && !x.IsDeleted);

        if (listRoleRequest.Name.HasValue)
        {
            var term = EscapeLikePattern(listRoleRequest.Name.Value.ToString());

            queryable = queryable
                .Where(x => EF.Functions.ILike(x.Name!, $"%{term}%", LikeEscapeCharacter));
        }

        if (listRoleRequest.IsBuiltIn.HasValue)
        {
            queryable = queryable
                .Where(x => x.IsBuiltIn == listRoleRequest.IsBuiltIn.Value);
        }

        return queryable;
    }

    protected override IQueryable<ApplicationRoleEntity> FilterByDateRange(IQueryable<ApplicationRoleEntity> queryable, DatedRequest datedRequest)
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

    private IQueryable<ApplicationRoleEntity> Detail()
    {
        return context.Roles.Include(x => x.RolePermissions);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter)
            .Replace("%", LikeEscapeCharacter + "%")
            .Replace("_", LikeEscapeCharacter + "_");
    }
}
