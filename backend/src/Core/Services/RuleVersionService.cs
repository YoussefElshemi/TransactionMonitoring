using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Services;

public class RuleVersionService(
    IRuleRepository ruleRepository,
    IRuleVersionRepository ruleVersionRepository,
    ITransactionSchemaService transactionSchemaService,
    IPartyMappingService partyMappingService,
    IVelocityProfileService velocityProfileService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : IRuleVersionService
{
    public Task<List<RuleVersion>> ListByIdsAsync(IReadOnlyCollection<RuleVersionId> ruleVersionIds, CancellationToken stoppingToken)
    {
        return ruleVersionRepository.ListByIdsAsync(ruleVersionIds, stoppingToken);
    }

    public async Task<RuleVersion> CreateVersionAsync(CreateRuleVersionRequest createRuleVersionRequest, CancellationToken stoppingToken)
    {
        var rule = await ruleRepository.GetAsync(createRuleVersionRequest.RuleId, stoppingToken);

        await transactionSchemaService.EnforceRuleBodyAsync(rule.OrganizationId, createRuleVersionRequest.RuleBody.ToModel(), stoppingToken);
        await partyMappingService.EnforceRuleBodyAsync(rule.OrganizationId, createRuleVersionRequest.RuleBody.ToModel(), stoppingToken);
        await velocityProfileService.EnforceRuleBodyAsync(rule.OrganizationId, createRuleVersionRequest.RuleBody.ToModel(), stoppingToken);

        var maxVersionNumber = await ruleVersionRepository.GetMaxVersionNumberAsync(rule.RuleId, stoppingToken);
        var nextVersionNumber = new VersionNumber(maxVersionNumber + 1);
        var ruleVersionId = new RuleVersionId(Guid.NewGuid());

        var ruleVersion = createRuleVersionRequest.ToVersion(rule, ruleVersionId, nextVersionNumber, timeProvider, modeContext.CurrentOrLive);
        await ruleVersionRepository.AddAsync(ruleVersion, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return ruleVersion;
    }

    public Task<RuleVersion> GetVersionAsync(RuleId ruleId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        return ruleVersionRepository.GetByVersionNumberAsync(ruleId, versionNumber, stoppingToken);
    }

    public Task<Exists> VersionExistsAsync(OrganizationId organizationId, RuleId ruleId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        return ruleVersionRepository.ExistsAsync(organizationId, ruleId, versionNumber, stoppingToken);
    }

    public Task<PagedResponse<RuleVersion>> ListAsync(ListRuleVersionRequest listRuleVersionRequest, CancellationToken stoppingToken)
    {
        return ruleVersionRepository.ListAsync(listRuleVersionRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListRuleVersionRequest listRuleVersionRequest, CancellationToken stoppingToken)
    {
        return ruleVersionRepository.GetCountAsync(listRuleVersionRequest, stoppingToken);
    }
}
