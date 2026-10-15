using Core.Enums;
using Core.Models.Cases;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ICaseSlaTargetRepository
{
    Task<Dictionary<RuleSeverity, SlaTarget>> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task SetAsync(SetCaseSlaTargetsRequest setCaseSlaTargetsRequest, CancellationToken stoppingToken);
}
