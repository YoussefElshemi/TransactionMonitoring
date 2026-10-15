using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IRuleVersionRepository : IPagedRepository<ListRuleVersionRequest, RuleVersion>
{
    Task AddAsync(RuleVersion ruleVersion, CancellationToken stoppingToken);
    Task<RuleVersion> GetAsync(RuleVersionId ruleVersionId, CancellationToken stoppingToken);
    Task<List<RuleVersion>> ListByIdsAsync(IReadOnlyCollection<RuleVersionId> ruleVersionIds, CancellationToken stoppingToken);
    Task<RuleVersion> GetByVersionNumberAsync(RuleId ruleId, VersionNumber versionNumber, CancellationToken stoppingToken);
    Task<VersionNumber> GetMaxVersionNumberAsync(RuleId ruleId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RuleId ruleId, VersionNumber versionNumber, CancellationToken stoppingToken);
}
