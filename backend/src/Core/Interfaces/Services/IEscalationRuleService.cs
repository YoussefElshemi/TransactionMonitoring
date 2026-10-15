using Core.Models.Escalations;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IEscalationRuleService : IPagedService<ListEscalationRuleRequest, EscalationRule>
{
    Task<EscalationRule> CreateAsync(CreateEscalationRuleRequest createEscalationRuleRequest, CancellationToken stoppingToken);
    Task<EscalationRule> GetAsync(EscalationRuleId escalationRuleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<EscalationRule?> FindByNameAsync(OrganizationId organizationId, EscalationRuleName name, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, EscalationRuleId escalationRuleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateEscalationRuleRequest updateEscalationRuleRequest, CancellationToken stoppingToken);
    Task DeleteAsync(EscalationRuleId escalationRuleId, CancellationToken stoppingToken);
    Task RecoverAsync(EscalationRuleId escalationRuleId, CancellationToken stoppingToken);

    Task<Count> EscalateDueAsync(CancellationToken stoppingToken);
}
