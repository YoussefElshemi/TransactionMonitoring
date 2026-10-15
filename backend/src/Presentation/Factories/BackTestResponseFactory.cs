using Core.Models.BackTests;
using Core.ValueObjects;
using Presentation.Mappers.BackTests;
using Presentation.Models.BackTests;

namespace Presentation.Factories;

public class BackTestResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<BackTestResourceDto> CreateAsync(BackTest backTest, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([backTest], context, stoppingToken);

        return resources[0];
    }

    public async Task<BackTestResourceDto[]> CreateAsync(
        IReadOnlyCollection<BackTest> backTests, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (backTests.Count == 0)
        {
            return [];
        }

        var rules = await relatedRecordLoader.LoadRulesAsync(
            backTests.Select(x => (RuleId?)x.RuleId), context, stoppingToken);
        var ruleVersions = await relatedRecordLoader.LoadRuleVersionsAsync(
            backTests.Select(x => (RuleVersionId?)x.RuleVersionId), context, stoppingToken);
        var createdByUsers = await relatedRecordLoader.LoadUsersAsync(
            backTests.Select(x => (ApplicationUserId?)x.CreatedByUserId), context, stoppingToken);

        return backTests.Select(x => new BackTestResourceDto
        {
            BackTest = x.ToDto(),
            Rule = rules.GetValueOrDefault(x.RuleId),
            RuleVersion = ruleVersions.GetValueOrDefault(x.RuleVersionId),
            CreatedByUser = createdByUsers.GetValueOrDefault(x.CreatedByUserId)
        }).ToArray();
    }
}
