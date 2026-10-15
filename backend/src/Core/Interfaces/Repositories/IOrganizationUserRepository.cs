using Core.Models.Organizations;
using Core.Models.Users;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IOrganizationUserRepository : IPagedRepository<ListOrganizationUserRequest, ApplicationUser>
{
    Task<Exists> ExistsByIdAsync(OrganizationId organizationId, ApplicationUserId userId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
}