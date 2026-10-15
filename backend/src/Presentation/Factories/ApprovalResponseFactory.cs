using Core.Models.Approvals;
using Core.ValueObjects;
using Presentation.Mappers.Approvals;
using Presentation.Models.Approvals;
using Presentation.Models.Rules;

namespace Presentation.Factories;

public class ApprovalResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<ApprovalResourceDto> CreateAsync(Approval approval, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([approval], context, stoppingToken);

        return resources[0];
    }

    public async Task<ApprovalResourceDto[]> CreateAsync(
        IReadOnlyCollection<Approval> approvals, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (approvals.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            approvals.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);

        var users = await relatedRecordLoader.LoadUsersAsync(
            approvals.Select(x => (ApplicationUserId?)x.RequestedByUserId)
                .Concat(approvals.Select(x => x.ReviewedByUserId)), context, stoppingToken);

        var ruleDeployments = await relatedRecordLoader.LoadRuleDeploymentsAsync(
            approvals.Select(x => x.RuleDeploymentId), context, stoppingToken);

        var rules = await relatedRecordLoader.LoadRulesAsync(
            ruleDeployments.Values.Select(x => (RuleId?)new RuleId(x.RuleId)), context, stoppingToken);

        var suspiciousActivityReports = await relatedRecordLoader.LoadSuspiciousActivityReportsAsync(
            approvals.Select(x => x.SuspiciousActivityReportId), context, stoppingToken);

        return approvals.Select(x => new ApprovalResourceDto
        {
            Approval = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId),
            RequestedByUser = users.GetValueOrDefault(x.RequestedByUserId),
            ReviewedByUser = x.ReviewedByUserId.HasValue ? users.GetValueOrDefault(x.ReviewedByUserId.Value) : null,
            RuleDeployment = x.RuleDeploymentId.HasValue
                ? ruleDeployments.GetValueOrDefault(x.RuleDeploymentId.Value)
                : null,
            Rule = ResolveRule(x, ruleDeployments, rules),
            SuspiciousActivityReport = x.SuspiciousActivityReportId.HasValue
                ? suspiciousActivityReports.GetValueOrDefault(x.SuspiciousActivityReportId.Value)
                : null
        }).ToArray();
    }

    private static RuleResponseDto? ResolveRule(
        Approval approval,
        IReadOnlyDictionary<RuleDeploymentId, RuleDeploymentResponseDto> ruleDeployments,
        IReadOnlyDictionary<RuleId, RuleResponseDto> rules)
    {
        if (!approval.RuleDeploymentId.HasValue)
        {
            return null;
        }

        var ruleDeployment = ruleDeployments.GetValueOrDefault(approval.RuleDeploymentId.Value);

        return ruleDeployment is null ? null : rules.GetValueOrDefault(new RuleId(ruleDeployment.RuleId));
    }
}
