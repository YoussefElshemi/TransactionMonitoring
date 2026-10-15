using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IRuleVersionService : IPagedService<ListRuleVersionRequest, RuleVersion>
{
    Task<RuleVersion> CreateVersionAsync(CreateRuleVersionRequest createRuleVersionRequest, CancellationToken stoppingToken);
    Task<RuleVersion> GetVersionAsync(RuleId ruleId, VersionNumber versionNumber, CancellationToken stoppingToken);
    Task<List<RuleVersion>> ListByIdsAsync(IReadOnlyCollection<RuleVersionId> ruleVersionIds, CancellationToken stoppingToken);
    Task<Exists> VersionExistsAsync(OrganizationId organizationId, RuleId ruleId, VersionNumber versionNumber, CancellationToken stoppingToken);
}
