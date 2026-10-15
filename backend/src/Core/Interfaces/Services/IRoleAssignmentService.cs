using Core.Models.Roles;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IRoleAssignmentService : IPagedService<ListRoleAssignmentRequest, RoleAssignment>
{
    Task<RoleAssignment> AssignAsync(AssignRoleRequest assignRoleRequest, CancellationToken stoppingToken);
    Task<RoleAssignment> GetAsync(RoleAssignmentId roleAssignmentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RoleAssignmentId roleAssignmentId, CancellationToken stoppingToken);
    Task<List<RoleAssignment>> ListForUserAsync(ApplicationUserId userId, CancellationToken stoppingToken);
    Task RevokeAsync(RevokeRoleRequest revokeRoleRequest, CancellationToken stoppingToken);
}
