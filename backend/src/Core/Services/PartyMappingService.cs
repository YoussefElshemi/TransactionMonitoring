using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.PartyMappings;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Services;

public class PartyMappingService(
    IPartyMappingRepository partyMappingRepository,
    IPartyMappingVersionRepository partyMappingVersionRepository,
    ITransactionSchemaRepository transactionSchemaRepository,
    IRuleRepository ruleRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : IPartyMappingService
{
    public async Task<PartyMapping> SetAsync(SetPartyMappingRequest setPartyMappingRequest, CancellationToken stoppingToken)
    {
        await EnforceSchemaAsync(setPartyMappingRequest, stoppingToken);

        await EnforceRulesSurviveAsync(setPartyMappingRequest.OrganizationId, setPartyMappingRequest.Mappings, stoppingToken);

        var existing = await partyMappingRepository.FindAsync(setPartyMappingRequest.OrganizationId, new IncludeDeleted(true), stoppingToken);
        var partyMappingVersionId = new PartyMappingVersionId(Guid.NewGuid());

        if (existing == null)
        {
            var partyMapping = setPartyMappingRequest.ToModel(partyMappingVersionId, timeProvider, modeContext.CurrentOrLive);

            await partyMappingRepository.AddAsync(partyMapping, stoppingToken);
            await partyMappingVersionRepository.AddAsync(
                setPartyMappingRequest.ToVersionModel(partyMappingVersionId, partyMapping.PartyMappingId, partyMapping.CurrentVersionNumber, timeProvider, modeContext.CurrentOrLive),
                stoppingToken);
        }
        else
        {
            var versionNumber = new VersionNumber(await partyMappingVersionRepository.GetMaxVersionNumberAsync(existing.PartyMappingId, stoppingToken) + 1);

            await partyMappingVersionRepository.AddAsync(
                setPartyMappingRequest.ToVersionModel(partyMappingVersionId, existing.PartyMappingId, versionNumber, timeProvider, modeContext.CurrentOrLive),
                stoppingToken);

            await partyMappingRepository.ReplaceMappingsAsync(
                setPartyMappingRequest.OrganizationId,
                setPartyMappingRequest.Mappings,
                partyMappingVersionId,
                versionNumber,
                stoppingToken);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(stoppingToken);
        }
        catch (UniqueConstraintViolationException exception)
        {
            // Read before written, so a concurrent Set loses the insert rather than overwriting.
            throw new PartyMappingConflictException(setPartyMappingRequest.OrganizationId, exception);
        }

        return await GetAsync(setPartyMappingRequest.OrganizationId, stoppingToken);
    }

    public async Task<PartyMapping> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var partyMapping = await partyMappingRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);

        return partyMapping ?? throw new InvalidOperationException($"No party mapping is defined for organisation '{organizationId}'.");
    }

    public async Task<PartyMapping> GetIncludingDeletedAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var partyMapping = await partyMappingRepository.FindAsync(organizationId, new IncludeDeleted(true), stoppingToken);

        return partyMapping ?? throw new InvalidOperationException($"No party mapping has ever been defined for organisation '{organizationId}'.");
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return partyMappingRepository.ExistsAsync(organizationId, includeDeleted, stoppingToken);
    }

    public async Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        await EnforceRulesSurviveAsync(organizationId, null, stoppingToken);

        await partyMappingRepository.DeleteAsync(organizationId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task<List<PartyMappingUsage>> ListUsagesAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var rules = await ruleRepository.ListByOrganizationAsync(organizationId, stoppingToken);

        return rules
            .Select(x => new { Rule = x, Roles = PartyMappingHelper.CollectRoles(x.RuleBody.ToModel()) })
            .Where(x => x.Roles.Count > 0)
            .Select(x => new PartyMappingUsage
            {
                RuleId = x.Rule.RuleId,
                FriendlyName = x.Rule.FriendlyName,
                IsEnabled = x.Rule.IsEnabled,
                Roles = x.Roles
            })
            .ToList();
    }

    private async Task EnforceRulesSurviveAsync(OrganizationId organizationId, List<PartyRoleMapping>? mappings, CancellationToken stoppingToken)
    {
        var rules = await ruleRepository.ListByOrganizationAsync(organizationId, stoppingToken);

        var prospective = mappings == null ? null : new PartyMapping
        {
            Mode = modeContext.CurrentOrLive,
            PartyMappingId = new PartyMappingId(Guid.Empty),
            OrganizationId = organizationId,
            Mappings = mappings,
            CurrentVersionId = new PartyMappingVersionId(Guid.Empty),
            CurrentVersionNumber = new VersionNumber(1),
            CreatedByUserId = new ApplicationUserId(Guid.Empty),
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };

        var stranded = new List<PartyMappingUsage>();

        foreach (var rule in rules)
        {
            var ruleBody = rule.RuleBody.ToModel();
            var roles = PartyMappingHelper.CollectRoles(ruleBody);

            if (roles.Count == 0 || PartyMappingHelper.ValidateRuleBody(ruleBody, prospective).Count == 0)
            {
                continue;
            }

            stranded.Add(new PartyMappingUsage
            {
                RuleId = rule.RuleId,
                FriendlyName = rule.FriendlyName,
                IsEnabled = rule.IsEnabled,
                Roles = roles
            });
        }

        if (stranded.Count > 0)
        {
            throw new PartyMappingInUseException(stranded);
        }
    }

    public async Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        await partyMappingRepository.RecoverAsync(organizationId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task<PartyMappingVersion> GetVersionAsync(OrganizationId organizationId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        var partyMapping = await GetIncludingDeletedAsync(organizationId, stoppingToken);

        return await partyMappingVersionRepository.GetByVersionNumberAsync(partyMapping.PartyMappingId, versionNumber, stoppingToken);
    }

    public Task<Exists> VersionExistsAsync(OrganizationId organizationId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        return partyMappingVersionRepository.ExistsAsync(organizationId, versionNumber, stoppingToken);
    }

    public Task<PagedResponse<PartyMappingVersion>> ListAsync(ListPartyMappingVersionRequest listPartyMappingVersionRequest, CancellationToken stoppingToken)
    {
        return partyMappingVersionRepository.ListAsync(listPartyMappingVersionRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListPartyMappingVersionRequest listPartyMappingVersionRequest, CancellationToken stoppingToken)
    {
        return partyMappingVersionRepository.GetCountAsync(listPartyMappingVersionRequest, stoppingToken);
    }

    public async Task EnforceRuleBodyAsync(OrganizationId organizationId, RuleBodyModel ruleBody, CancellationToken stoppingToken)
    {
        var partyMapping = await partyMappingRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);

        var violations = PartyMappingHelper.ValidateRuleBody(ruleBody, partyMapping);

        if (violations.Count > 0)
        {
            throw new PartyRuleViolationException(violations);
        }
    }

    private async Task EnforceSchemaAsync(SetPartyMappingRequest setPartyMappingRequest, CancellationToken stoppingToken)
    {
        var transactionSchema = await transactionSchemaRepository.FindAsync(setPartyMappingRequest.OrganizationId, new IncludeDeleted(false), stoppingToken);

        if (transactionSchema == null)
        {
            return;
        }

        var violations = TransactionSchemaHelper.ValidatePartyMapping(setPartyMappingRequest.Mappings, transactionSchema);

        if (violations.Count > 0)
        {
            throw new PartyMappingSchemaViolationException(violations);
        }
    }
}
