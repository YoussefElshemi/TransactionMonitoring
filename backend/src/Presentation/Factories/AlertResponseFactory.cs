using Core.Models.Alerts;
using Core.ValueObjects;
using Presentation.Mappers.Alerts;
using Presentation.Models.Alerts;

namespace Presentation.Factories;

public class AlertResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<AlertResourceDto> CreateAsync(Alert alert, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([alert], context, stoppingToken);

        return resources[0];
    }

    public async Task<AlertResourceDto[]> CreateAsync(
        IReadOnlyCollection<Alert> alerts, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (alerts.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            alerts.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);
        var rules = await relatedRecordLoader.LoadRulesAsync(
            alerts.Select(x => (RuleId?)x.RuleId), context, stoppingToken);
        var transactions = await relatedRecordLoader.LoadTransactionsAsync(
            alerts.Select(x => (TransactionId?)x.TransactionId), context, stoppingToken);

        return alerts.Select(x => new AlertResourceDto
        {
            Alert = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId),
            Rule = rules.GetValueOrDefault(x.RuleId),
            Transaction = transactions.GetValueOrDefault(x.TransactionId)
        }).ToArray();
    }
}
