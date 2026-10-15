using Core.Models.Permissions;
using Core.Models.Roles;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IRoleAssignmentRepository : IPagedRepository<ListRoleAssignmentRequest, RoleAssignment>
{
    Task AddAsync(RoleAssignment roleAssignment, CancellationToken stoppingToken);
    Task<RoleAssignment> GetAsync(RoleAssignmentId roleAssignmentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RoleAssignmentId roleAssignmentId, CancellationToken stoppingToken);

    Task<RoleAssignment?> FindAsync(ApplicationUserId userId, RoleId roleId, PermissionScope scope, CancellationToken stoppingToken);

    Task<List<RoleAssignment>> ListForUserAsync(ApplicationUserId userId, CancellationToken stoppingToken);
    Task DeleteAsync(RoleAssignmentId roleAssignmentId, CancellationToken stoppingToken);

    Task<Count> DeleteForUserAsync(ApplicationUserId userId, CancellationToken stoppingToken);
}
