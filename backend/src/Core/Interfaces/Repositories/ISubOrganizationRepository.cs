using Core.Models.SubOrganizations;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ISubOrganizationRepository : IPagedRepository<ListSubOrganizationRequest, SubOrganization>
{
    Task AddAsync(SubOrganization subOrganization, CancellationToken stoppingToken);
    Task<SubOrganization> GetAsync(SubOrganizationId subOrganizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<SubOrganization?> FindByNameAsync(OrganizationId organizationId, SubOrganizationName name, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, SubOrganizationId subOrganizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<SubOrganizationUsage> GetUsageAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateSubOrganizationRequest updateSubOrganizationRequest, CancellationToken stoppingToken);
    Task DeleteAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken);
    Task RecoverAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken);
}
