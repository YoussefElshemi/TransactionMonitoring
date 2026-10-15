using Core.Enums;
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

public class RoleService(
    IRoleRepository roleRepository,
    IPermissionService permissionService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRoleService
{
    public async Task<Role> CreateAsync(CreateRoleRequest createRoleRequest, CancellationToken stoppingToken)
    {
        await EnsureGrantableAsync(createRoleRequest.ComposedByUserId, createRoleRequest.Permissions, stoppingToken);

        var role = createRoleRequest.ToModel(timeProvider);

        await roleRepository.AddAsync(role, stoppingToken);

        await roleRepository.SetPermissionsAsync(role.RoleId, role.Permissions, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(role.RoleId, new IncludeDeleted(false), stoppingToken);
    }

    public Task<Role> GetAsync(RoleId roleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return roleRepository.GetAsync(roleId, includeDeleted, stoppingToken);
    }

    public Task<Role?> FindByNameAsync(OrganizationId organizationId, RoleName name, CancellationToken stoppingToken)
    {
        return roleRepository.FindByNameAsync(organizationId, name, stoppingToken);
    }

    public Task<PagedResponse<Role>> ListAsync(ListRoleRequest listRoleRequest, CancellationToken stoppingToken)
    {
        return roleRepository.ListAsync(listRoleRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListRoleRequest listRoleRequest, CancellationToken stoppingToken)
    {
        return roleRepository.GetCountAsync(listRoleRequest, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, RoleId roleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return roleRepository.ExistsAsync(organizationId, roleId, includeDeleted, stoppingToken);
    }

    public Task<RoleUsage> GetUsageAsync(RoleId roleId, CancellationToken stoppingToken)
    {
        return roleRepository.GetUsageAsync(roleId, stoppingToken);
    }

    public async Task UpdateAsync(UpdateRoleRequest updateRoleRequest, CancellationToken stoppingToken)
    {
        var role = await roleRepository.GetAsync(updateRoleRequest.RoleId, new IncludeDeleted(false), stoppingToken);

        ThrowIfBuiltIn(role);

        if (updateRoleRequest.Permissions is { } permissions)
        {
            await EnsureGrantableAsync(updateRoleRequest.ComposedByUserId, permissions, stoppingToken);

            await roleRepository.SetPermissionsAsync(role.RoleId, permissions, stoppingToken);
        }

        await roleRepository.UpdateAsync(updateRoleRequest, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task DeleteAsync(RoleId roleId, CancellationToken stoppingToken)
    {
        var role = await roleRepository.GetAsync(roleId, new IncludeDeleted(false), stoppingToken);

        ThrowIfBuiltIn(role);

        var usage = await roleRepository.GetUsageAsync(roleId, stoppingToken);

        if (usage.AssignmentCount > 0)
        {
            throw new RoleInUseException(usage);
        }

        await roleRepository.DeleteAsync(roleId, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(RoleId roleId, CancellationToken stoppingToken)
    {
        await roleRepository.RecoverAsync(roleId, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    private static void ThrowIfBuiltIn(Role role)
    {
        if (role.IsBuiltIn)
        {
            throw new BuiltInRoleException(role.Name);
        }
    }

    private async Task EnsureGrantableAsync(ApplicationUserId composedByUserId, IReadOnlyCollection<Permission> permissions, CancellationToken stoppingToken)
    {
        var platformPermissions = permissions.Where(x => !PermissionBundle.IsGrantableToCustomRole(x)).ToList();

        if (platformPermissions.Count > 0)
        {
            throw new PermissionNotGrantableException(platformPermissions, true);
        }

        var composer = await permissionService.ResolveAsync(composedByUserId, stoppingToken);

        var notHeld = permissions.Where(x => !composer.Holds(x)).ToList();

        if (notHeld.Count > 0)
        {
            throw new PermissionNotGrantableException(notHeld, false);
        }
    }
}
