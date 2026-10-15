using Core.Models.Roles;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IRoleService : IPagedService<ListRoleRequest, Role>
{
    Task<Role> CreateAsync(CreateRoleRequest createRoleRequest, CancellationToken stoppingToken);
    Task<Role> GetAsync(RoleId roleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Role?> FindByNameAsync(OrganizationId organizationId, RoleName name, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RoleId roleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<RoleUsage> GetUsageAsync(RoleId roleId, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateRoleRequest updateRoleRequest, CancellationToken stoppingToken);
    Task DeleteAsync(RoleId roleId, CancellationToken stoppingToken);
    Task RecoverAsync(RoleId roleId, CancellationToken stoppingToken);
}
