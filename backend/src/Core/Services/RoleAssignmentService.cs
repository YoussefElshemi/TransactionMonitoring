using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Roles;
using Core.ValueObjects;

namespace Core.Services;

public class RoleAssignmentService(
    IRoleAssignmentRepository roleAssignmentRepository,
    IRoleRepository roleRepository,
    IPermissionService permissionService,
    IPermissionScopeRepository permissionScopeRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRoleAssignmentService
{
    public async Task<RoleAssignment> AssignAsync(AssignRoleRequest assignRoleRequest, CancellationToken stoppingToken)
    {
        await EnsureGrantableAsync(assignRoleRequest.AssignedByUserId, assignRoleRequest.RoleId, stoppingToken);

        var isWithinOrganization = await permissionScopeRepository.IsScopeWithinOrganizationAsync(
            assignRoleRequest.OrganizationId, assignRoleRequest.Scope, stoppingToken);

        if (!isWithinOrganization)
        {
            throw new CrossOrganizationScopeException();
        }

        var existing = await roleAssignmentRepository.FindAsync(
            assignRoleRequest.UserId, assignRoleRequest.RoleId, assignRoleRequest.Scope, stoppingToken);

        if (existing is not null)
        {
            return existing;
        }

        var roleAssignment = assignRoleRequest.ToModel(timeProvider);

        await roleAssignmentRepository.AddAsync(roleAssignment, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(roleAssignment.RoleAssignmentId, stoppingToken);
    }

    public Task<RoleAssignment> GetAsync(RoleAssignmentId roleAssignmentId, CancellationToken stoppingToken)
    {
        return roleAssignmentRepository.GetAsync(roleAssignmentId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, RoleAssignmentId roleAssignmentId, CancellationToken stoppingToken)
    {
        return roleAssignmentRepository.ExistsAsync(organizationId, roleAssignmentId, stoppingToken);
    }

    public Task<List<RoleAssignment>> ListForUserAsync(ApplicationUserId userId, CancellationToken stoppingToken)
    {
        return roleAssignmentRepository.ListForUserAsync(userId, stoppingToken);
    }

    public Task<PagedResponse<RoleAssignment>> ListAsync(ListRoleAssignmentRequest listRoleAssignmentRequest, CancellationToken stoppingToken)
    {
        return roleAssignmentRepository.ListAsync(listRoleAssignmentRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListRoleAssignmentRequest listRoleAssignmentRequest, CancellationToken stoppingToken)
    {
        return roleAssignmentRepository.GetCountAsync(listRoleAssignmentRequest, stoppingToken);
    }

    public async Task RevokeAsync(RevokeRoleRequest revokeRoleRequest, CancellationToken stoppingToken)
    {
        await roleAssignmentRepository.DeleteAsync(revokeRoleRequest.RoleAssignmentId, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    private async Task EnsureGrantableAsync(ApplicationUserId assignedByUserId, RoleId roleId, CancellationToken stoppingToken)
    {
        var role = await roleRepository.GetAsync(roleId, new IncludeDeleted(false), stoppingToken);

        var platformPermissions = role.Permissions.Where(x => !PermissionBundle.IsGrantableToCustomRole(x)).ToList();

        if (platformPermissions.Count > 0)
        {
            throw new PermissionNotGrantableException(platformPermissions, true);
        }

        var assigner = await permissionService.ResolveAsync(assignedByUserId, stoppingToken);

        var notHeld = role.Permissions.Where(x => !assigner.Holds(x)).ToList();

        if (notHeld.Count > 0)
        {
            throw new PermissionNotGrantableException(notHeld, false);
        }
    }
}
