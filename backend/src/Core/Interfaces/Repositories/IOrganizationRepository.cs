using Core.Models.Organizations;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IOrganizationRepository : IPagedRepository<ListOrganizationRequest, Organization>
{
    Task AddAsync(Organization organization, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateOrganizationRequest updateOrganizationRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Organization> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<List<Organization>> ListByIdsAsync(IReadOnlyCollection<OrganizationId> organizationIds, CancellationToken stoppingToken);
    Task<List<Organization>> GetAllAsync(CancellationToken stoppingToken);
    Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken);
}