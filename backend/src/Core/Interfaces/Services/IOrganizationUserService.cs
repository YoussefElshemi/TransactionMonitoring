using Core.Models.Organizations;
using Core.Models.Users;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IOrganizationUserService : IPagedService<ListOrganizationUserRequest, ApplicationUser>
{
    Task<Exists> ExistsAsync(OrganizationId organizationId, ApplicationUserId userId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
}