using System.Collections.ObjectModel;
using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.ApiClients;
using Presentation.Mappers.Organizations;
using Presentation.Mappers.Rules;
using Presentation.Mappers.RuleSets;
using Presentation.Mappers.SuspiciousActivityReports;
using Presentation.Mappers.Transactions;
using Presentation.Mappers.Users;
using Presentation.Models.ApiClients;
using Presentation.Models.Organizations;
using Presentation.Models.Rules;
using Presentation.Models.RuleSets;
using Presentation.Models.SuspiciousActivityReports;
using Presentation.Models.Transactions;
using Presentation.Models.Users;

namespace Presentation.Factories;

public class RelatedRecordLoader(
    IOrganizationService organizationService,
    IApplicationUserService applicationUserService,
    IRuleService ruleService,
    IRuleSetService ruleSetService,
    ITransactionService transactionService,
    IRuleVersionService ruleVersionService,
    IRuleDeploymentService ruleDeploymentService,
    IApiClientService apiClientService,
    ISuspiciousActivityReportService suspiciousActivityReportService,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyDictionary<OrganizationId, OrganizationResponseDto>> LoadOrganizationsAsync(
        IEnumerable<OrganizationId?> organizationIds, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var ids = Distinct(organizationIds);

        if (ids.Count == 0)
        {
            return ReadOnlyDictionary<OrganizationId, OrganizationResponseDto>.Empty;
        }

        var organizations = await organizationService.ListByIdsAsync(ids, stoppingToken);

        return organizations
            .Where(x => context.CanDisclose(x.OrganizationId))
            .ToDictionary(x => x.OrganizationId, x => x.ToDto());
    }

    public async Task<IReadOnlyDictionary<ApplicationUserId, UserResponseDto>> LoadUsersAsync(
        IEnumerable<ApplicationUserId?> userIds, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var ids = Distinct(userIds);

        if (ids.Count == 0)
        {
            return ReadOnlyDictionary<ApplicationUserId, UserResponseDto>.Empty;
        }

        var users = await applicationUserService.ListByIdsAsync(ids, stoppingToken);

        return users
            .Where(x => context.CanDisclose(x.OrganizationId))
            .ToDictionary(x => x.UserId, x => x.ToDto());
    }

    public async Task<IReadOnlyDictionary<RuleId, RuleResponseDto>> LoadRulesAsync(
        IEnumerable<RuleId?> ruleIds, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var ids = Distinct(ruleIds);

        if (ids.Count == 0)
        {
            return ReadOnlyDictionary<RuleId, RuleResponseDto>.Empty;
        }

        var rules = await ruleService.ListByIdsAsync(ids, stoppingToken);

        return rules
            .Where(x => context.CanDisclose(x.OrganizationId))
            .ToDictionary(x => x.RuleId, x => x.ToDto());
    }

    public async Task<IReadOnlyDictionary<RuleDeploymentId, RuleDeploymentResponseDto>> LoadRuleDeploymentsAsync(
        IEnumerable<RuleDeploymentId?> ruleDeploymentIds, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var ids = Distinct(ruleDeploymentIds);

        if (ids.Count == 0)
        {
            return ReadOnlyDictionary<RuleDeploymentId, RuleDeploymentResponseDto>.Empty;
        }

        var ruleDeployments = await ruleDeploymentService.ListByIdsAsync(ids, stoppingToken);

        return ruleDeployments
            .Where(x => context.CanDisclose(x.OrganizationId))
            .ToDictionary(x => x.RuleDeploymentId, x => x.ToDto());
    }

    public async Task<IReadOnlyDictionary<RuleSetId, RuleSetResponseDto>> LoadRuleSetsAsync(
        IEnumerable<RuleSetId?> ruleSetIds, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var ids = Distinct(ruleSetIds);

        if (ids.Count == 0)
        {
            return ReadOnlyDictionary<RuleSetId, RuleSetResponseDto>.Empty;
        }

        var ruleSets = await ruleSetService.ListByIdsAsync(ids, stoppingToken);

        return ruleSets
            .Where(x => context.CanDisclose(x.OrganizationId))
            .ToDictionary(x => x.RuleSetId, x => x.ToDto());
    }

    public async Task<IReadOnlyDictionary<TransactionId, TransactionSummaryResponseDto>> LoadTransactionsAsync(
        IEnumerable<TransactionId?> transactionIds, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var ids = Distinct(transactionIds);

        if (ids.Count == 0)
        {
            return ReadOnlyDictionary<TransactionId, TransactionSummaryResponseDto>.Empty;
        }

        var transactions = await transactionService.ListByIdsAsync(ids, stoppingToken);

        return transactions
            .Where(x => context.CanDisclose(x.OrganizationId))
            .ToDictionary(x => x.TransactionId, x => x.ToSummaryDto());
    }

    public async Task<IReadOnlyDictionary<RuleVersionId, RuleVersionResponseDto>> LoadRuleVersionsAsync(
        IEnumerable<RuleVersionId?> ruleVersionIds, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var ids = Distinct(ruleVersionIds);

        if (ids.Count == 0)
        {
            return ReadOnlyDictionary<RuleVersionId, RuleVersionResponseDto>.Empty;
        }

        var ruleVersions = await ruleVersionService.ListByIdsAsync(ids, stoppingToken);

        return ruleVersions
            .Where(x => context.CanDisclose(x.OrganizationId))
            .ToDictionary(x => x.RuleVersionId, x => x.ToDto());
    }

    public async Task<IReadOnlyDictionary<ApiClientId, ApiClientResponseDto>> LoadApiClientsAsync(
        IEnumerable<ApiClientId?> apiClientIds, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var ids = Distinct(apiClientIds);

        if (ids.Count == 0)
        {
            return ReadOnlyDictionary<ApiClientId, ApiClientResponseDto>.Empty;
        }

        var apiClients = await apiClientService.ListByIdsAsync(ids, stoppingToken);

        return apiClients
            .Where(x => context.CanDisclose(x.OrganizationId))
            .ToDictionary(x => x.ApiClientId, x => x.ToDto());
    }

    public async Task<IReadOnlyDictionary<SuspiciousActivityReportId, SuspiciousActivityReportResponseDto>> LoadSuspiciousActivityReportsAsync(
        IEnumerable<SuspiciousActivityReportId?> suspiciousActivityReportIds, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var ids = Distinct(suspiciousActivityReportIds);

        if (ids.Count == 0)
        {
            return ReadOnlyDictionary<SuspiciousActivityReportId, SuspiciousActivityReportResponseDto>.Empty;
        }

        var suspiciousActivityReports = await suspiciousActivityReportService.ListByIdsAsync(ids, stoppingToken);

        var now = timeProvider.GetUtcNow();

        return suspiciousActivityReports
            .Where(x => context.CanDisclose(x.OrganizationId))
            .ToDictionary(x => x.SuspiciousActivityReportId, x => x.ToDto(now));
    }

    private static List<T> Distinct<T>(IEnumerable<T?> ids)
        where T : struct
    {
        return ids.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
    }
}
