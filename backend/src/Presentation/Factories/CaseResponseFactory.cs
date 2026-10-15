using Core.Helpers;
using Core.Models.Cases;
using Core.ValueObjects;
using Presentation.Mappers.Cases;
using Presentation.Models.Cases;

namespace Presentation.Factories;

public class CaseResponseFactory(RelatedRecordLoader relatedRecordLoader, TimeProvider timeProvider)
{
    public async Task<CaseResourceDto> CreateAsync(Case @case, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([@case], context, stoppingToken);

        return resources[0];
    }

    public async Task<CaseResourceDto[]> CreateAsync(
        IReadOnlyCollection<Case> cases, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (cases.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            cases.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);
        var assignedToUsers = await relatedRecordLoader.LoadUsersAsync(
            cases.Select(x => x.AssignedToUserId), context, stoppingToken);
        var transactions = await relatedRecordLoader.LoadTransactionsAsync(
            cases.Select(x => x.TransactionId), context, stoppingToken);
        var rules = await relatedRecordLoader.LoadRulesAsync(
            cases.SelectMany(x => x.RuleEvaluations).Select(x => (RuleId?)x.RuleId), context, stoppingToken);

        var now = timeProvider.GetUtcNow();

        return cases.Select(x => new CaseResourceDto
        {
            Case = x.ToDto(),
            Ageing = CaseAgeingResolver.Resolve(x, now).ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId),
            AssignedToUser = x.AssignedToUserId.HasValue ? assignedToUsers.GetValueOrDefault(x.AssignedToUserId.Value) : null,
            Transaction = x.TransactionId.HasValue ? transactions.GetValueOrDefault(x.TransactionId.Value) : null,
            Rules = ResourceRelations.Resolve(x.RuleEvaluations.Select(evaluation => evaluation.RuleId), rules)
        }).ToArray();
    }
}
