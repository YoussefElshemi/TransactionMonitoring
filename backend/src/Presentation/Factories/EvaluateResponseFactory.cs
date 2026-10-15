using Core.Models.Cases;
using Core.Models.Decisions;
using Core.Models.Rules;
using Core.Models.RuleSets;
using Core.ValueObjects;
using Presentation.Mappers.Rules;
using Presentation.Mappers.RuleSets;
using Presentation.Models.Rules;
using Presentation.Models.RuleSets;

namespace Presentation.Factories;

public class EvaluateResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<EvaluateRuleSetResourceDto> CreateAsync(
        EvaluateRuleSetResponse response, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var ruleSets = await relatedRecordLoader.LoadRuleSetsAsync([response.RuleSetId], context, stoppingToken);

        var ruleIds = response.Results.Select(x => (RuleId?)x.RuleId)
            .Concat(RuleIdsOf(response.Case, response.Decision))
            .ToList();
        var rules = await relatedRecordLoader.LoadRulesAsync(ruleIds, context, stoppingToken);

        var userIds = UserIdsOf(response.Case, response.Decision);
        var users = await relatedRecordLoader.LoadUsersAsync(userIds, context, stoppingToken);

        return new EvaluateRuleSetResourceDto
        {
            Result = response.ToDto(),
            RuleSet = ruleSets.GetValueOrDefault(response.RuleSetId),
            Rules = ResourceRelations.Resolve(ruleIds.OfType<RuleId>(), rules),
            Users = ResourceRelations.Resolve(userIds.OfType<ApplicationUserId>(), users)
        };
    }

    public async Task<EvaluateRuleResourceDto> CreateAsync(
        EvaluateRuleResponse response, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var ruleIds = new[] { (RuleId?)response.RuleId }
            .Concat(RuleIdsOf(response.Case, response.Decision))
            .ToList();
        var rules = await relatedRecordLoader.LoadRulesAsync(ruleIds, context, stoppingToken);

        var userIds = UserIdsOf(response.Case, response.Decision);
        var users = await relatedRecordLoader.LoadUsersAsync(userIds, context, stoppingToken);

        return new EvaluateRuleResourceDto
        {
            Result = response.ToDto(),
            Rules = ResourceRelations.Resolve(ruleIds.OfType<RuleId>(), rules),
            Users = ResourceRelations.Resolve(userIds.OfType<ApplicationUserId>(), users)
        };
    }

    public async Task<EvaluateAllRulesResourceDto> CreateAsync(
        EvaluateAllRulesResponse response, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var ruleIds = response.Results.Select(x => (RuleId?)x.RuleId)
            .Concat(RuleIdsOf(response.Case, response.Decision))
            .ToList();
        var rules = await relatedRecordLoader.LoadRulesAsync(ruleIds, context, stoppingToken);

        var userIds = UserIdsOf(response.Case, response.Decision);
        var users = await relatedRecordLoader.LoadUsersAsync(userIds, context, stoppingToken);

        return new EvaluateAllRulesResourceDto
        {
            Result = response.ToDto(),
            Rules = ResourceRelations.Resolve(ruleIds.OfType<RuleId>(), rules),
            Users = ResourceRelations.Resolve(userIds.OfType<ApplicationUserId>(), users)
        };
    }

    private static List<RuleId?> RuleIdsOf(Case? @case, Decision? decision)
    {
        return (@case?.RuleEvaluations ?? [])
            .Concat(decision?.RuleEvaluations ?? [])
            .Select(x => (RuleId?)x.RuleId)
            .ToList();
    }

    private static List<ApplicationUserId?> UserIdsOf(Case? @case, Decision? decision)
    {
        return [@case?.AssignedToUserId, decision?.ResolvedByUserId];
    }
}
