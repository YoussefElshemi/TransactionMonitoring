using Core.Models.CaseLinks;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ICaseLinkRepository : IPagedRepository<ListCaseLinkRequest, CaseLink>
{
    Task AddAsync(CaseLink caseLink, CancellationToken stoppingToken);

    Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, CaseLinkId caseLinkId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);

    Task<Exists> ExistsBetweenAsync(OrganizationId organizationId, CaseId caseId, CaseId linkedCaseId, CancellationToken stoppingToken);

    Task<CaseLink> GetAsync(CaseLinkId caseLinkId, CaseId viewpointCaseId, CancellationToken stoppingToken);
    Task DeleteAsync(CaseLinkId caseLinkId, CancellationToken stoppingToken);
}
