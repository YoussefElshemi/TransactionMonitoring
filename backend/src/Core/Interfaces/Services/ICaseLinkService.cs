using Core.Models.CaseLinks;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface ICaseLinkService : IPagedService<ListCaseLinkRequest, CaseLink>
{
    Task<CaseLink> CreateAsync(CreateCaseLinkRequest createCaseLinkRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, CaseLinkId caseLinkId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<CaseLink> GetAsync(CaseLinkId caseLinkId, CaseId viewpointCaseId, CancellationToken stoppingToken);
    Task DeleteAsync(CaseLinkId caseLinkId, CancellationToken stoppingToken);
}
