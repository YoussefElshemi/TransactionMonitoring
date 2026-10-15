using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Permissions;
using Core.Models.Roles;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RoleAssignmentRepository(TransactionMonitoringDbContext context)
    : PagedRepository<ListRoleAssignmentRequest, RoleAssignmentEntity, RoleAssignment>, IRoleAssignmentRepository
{
    public async Task AddAsync(RoleAssignment roleAssignment, CancellationToken stoppingToken)
    {
        await context.RoleAssignments.AddAsync(roleAssignment.ToEntity(), stoppingToken);
    }

    public async Task<RoleAssignment> GetAsync(RoleAssignmentId roleAssignmentId, CancellationToken stoppingToken)
    {
        var roleAssignmentEntity = await Detail()
            .FirstAsync(x => x.RoleAssignmentId == roleAssignmentId, stoppingToken);

        return roleAssignmentEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, RoleAssignmentId roleAssignmentId, CancellationToken stoppingToken)
    {
        var exists = await context.RoleAssignments
            .AnyAsync(x => x.RoleAssignmentId == roleAssignmentId &&
                           x.User.OrganizationId == organizationId, stoppingToken);

        return new Exists(exists);
    }

    public async Task<RoleAssignment?> FindAsync(ApplicationUserId userId, RoleId roleId, PermissionScope scope, CancellationToken stoppingToken)
    {
        var scopeTypeId = (int)scope.ScopeType;
        var subOrganizationId = scope.SubOrganizationId.HasValue ? (Guid?)scope.SubOrganizationId.Value : null;
        var ruleSetId = scope.RuleSetId.HasValue ? (Guid?)scope.RuleSetId.Value : null;
        var queueId = scope.QueueId.HasValue ? (Guid?)scope.QueueId.Value : null;

        var roleAssignmentEntity = await Detail()
            .FirstOrDefaultAsync(x => x.UserId == userId &&
                                      x.RoleId == roleId &&
                                      x.ScopeTypeId == scopeTypeId &&
                                      x.SubOrganizationId == subOrganizationId &&
                                      x.RuleSetId == ruleSetId &&
                                      x.QueueId == queueId, stoppingToken);

        return roleAssignmentEntity?.ToModel();
    }

    public async Task<List<RoleAssignment>> ListForUserAsync(ApplicationUserId userId, CancellationToken stoppingToken)
    {
        var roleAssignmentEntities = await Detail()
            .Where(x => x.UserId == userId)
            .ToListAsync(stoppingToken);

        return roleAssignmentEntities.ConvertAll(x => x.ToModel());
    }

    public async Task DeleteAsync(RoleAssignmentId roleAssignmentId, CancellationToken stoppingToken)
    {
        var roleAssignmentEntity = await context.RoleAssignments
            .FirstAsync(x => x.RoleAssignmentId == roleAssignmentId, stoppingToken);

        context.RoleAssignments.Remove(roleAssignmentEntity);
    }

    // Through the tracker, not ExecuteDelete, so the audit interceptor sees each grant go.
    public async Task<Count> DeleteForUserAsync(ApplicationUserId userId, CancellationToken stoppingToken)
    {
        Guid userIdValue = userId;

        var roleAssignmentEntities = await context.RoleAssignments
            .Where(x => x.UserId == userIdValue)
            .ToListAsync(stoppingToken);

        context.RoleAssignments.RemoveRange(roleAssignmentEntities);

        return new Count(roleAssignmentEntities.Count);
    }

    protected override Task<List<RoleAssignment>> MapPageAsync(List<RoleAssignmentEntity> items, ListRoleAssignmentRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<RoleAssignmentEntity> OrderByExpression()
    {
        return new OrderByExpression<RoleAssignmentEntity, DateTimeOffset>(x => x.CreatedAt, true, x => x.CreatedAt);
    }

    protected override IQueryable<RoleAssignmentEntity> FilterByPagedRequest(ListRoleAssignmentRequest listRoleAssignmentRequest)
    {
        var queryable = Detail()
            .Where(x => x.User.OrganizationId == listRoleAssignmentRequest.OrganizationId);

        if (listRoleAssignmentRequest.UserId.HasValue)
        {
            Guid userId = listRoleAssignmentRequest.UserId.Value;

            queryable = queryable.Where(x => x.UserId == userId);
        }

        if (listRoleAssignmentRequest.RoleId.HasValue)
        {
            Guid roleId = listRoleAssignmentRequest.RoleId.Value;

            queryable = queryable.Where(x => x.RoleId == roleId);
        }

        return queryable;
    }

    protected override IQueryable<RoleAssignmentEntity> FilterByDateRange(IQueryable<RoleAssignmentEntity> queryable, DatedRequest datedRequest)
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

    private IQueryable<RoleAssignmentEntity> Detail()
    {
        return context.RoleAssignments
            .Include(x => x.Role)
            .ThenInclude(x => x.RolePermissions);
    }
}
