using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Common;
using Core.Models.Organizations;
using Core.Models.Users;
using Core.ValueObjects;

namespace Core.Services;

public class OrganizationUserService(IOrganizationUserRepository organizationUserRepository) : IOrganizationUserService
{
    public Task<PagedResponse<ApplicationUser>> ListAsync(ListOrganizationUserRequest listOrganizationUserRequest, CancellationToken stoppingToken)
    {
        return organizationUserRepository.ListAsync(listOrganizationUserRequest, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, ApplicationUserId userId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return organizationUserRepository.ExistsByIdAsync(organizationId, userId, includeDeleted, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListOrganizationUserRequest listOrganizationUserRequest, CancellationToken stoppingToken)
    {
        return organizationUserRepository.GetCountAsync(listOrganizationUserRequest, stoppingToken);
    }
}