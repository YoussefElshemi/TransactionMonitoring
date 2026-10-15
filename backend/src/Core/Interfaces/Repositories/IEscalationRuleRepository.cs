using Core.Models.Escalations;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IEscalationRuleRepository : IPagedRepository<ListEscalationRuleRequest, EscalationRule>
{
    Task AddAsync(EscalationRule escalationRule, CancellationToken stoppingToken);
    Task<EscalationRule> GetAsync(EscalationRuleId escalationRuleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<EscalationRule?> FindByNameAsync(OrganizationId organizationId, EscalationRuleName name, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, EscalationRuleId escalationRuleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<List<EscalationRule>> ListActiveAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task<List<OrganizationId>> ListOrganizationsWithActiveRulesAsync(CancellationToken stoppingToken);
    Task UpdateAsync(UpdateEscalationRuleRequest updateEscalationRuleRequest, CancellationToken stoppingToken);
    Task DeleteAsync(EscalationRuleId escalationRuleId, CancellationToken stoppingToken);
    Task RecoverAsync(EscalationRuleId escalationRuleId, CancellationToken stoppingToken);
}
