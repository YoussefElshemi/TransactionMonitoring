using Core.Models.Transactions;
using Core.ValueObjects;
using Presentation.Mappers.Transactions;
using Presentation.Models.Transactions;

namespace Presentation.Factories;

public class TransactionResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<TransactionResourceDto> CreateAsync(
        Transaction transaction, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([transaction], context, stoppingToken);

        return resources[0];
    }

    public async Task<TransactionResourceDto> CreateRevealedAsync(
        Transaction transaction, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([transaction], context, stoppingToken);

        return resources[0] with { Transaction = transaction.ToRevealedDto() };
    }

    public async Task<TransactionResourceDto[]> CreateAsync(
        IReadOnlyCollection<Transaction> transactions, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (transactions.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            transactions.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);
        var rules = await relatedRecordLoader.LoadRulesAsync(
            transactions.SelectMany(RuleIdsOf), context, stoppingToken);
        var users = await relatedRecordLoader.LoadUsersAsync(
            transactions.SelectMany(UserIdsOf), context, stoppingToken);

        return transactions.Select(x => new TransactionResourceDto
        {
            Transaction = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId),
            Rules = ResourceRelations.Resolve(RuleIdsOf(x).OfType<RuleId>(), rules),
            Users = ResourceRelations.Resolve(UserIdsOf(x).OfType<ApplicationUserId>(), users)
        }).ToArray();
    }

    private static IEnumerable<RuleId?> RuleIdsOf(Transaction transaction)
    {
        return transaction.RuleEvaluations
            .Concat(transaction.Cases.SelectMany(x => x.RuleEvaluations))
            .Concat(transaction.Decision?.RuleEvaluations ?? [])
            .Select(x => (RuleId?)x.RuleId);
    }

    private static IEnumerable<ApplicationUserId?> UserIdsOf(Transaction transaction)
    {
        return transaction.Cases
            .Select(x => x.AssignedToUserId)
            .Append(transaction.Decision?.ResolvedByUserId);
    }
}
