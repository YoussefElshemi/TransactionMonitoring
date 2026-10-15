using Core.Helpers;
using Core.Interfaces.Services;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;
using Presentation.Mappers.SuspiciousActivityReports;
using Presentation.Models.SuspiciousActivityReports;

namespace Presentation.Factories;

public class SuspiciousActivityReportResponseFactory(
    RelatedRecordLoader relatedRecordLoader,
    ISuspiciousActivityReportService suspiciousActivityReportService,
    TimeProvider timeProvider)
{
    public async Task<SuspiciousActivityReportResourceDto> CreateAsync(
        SuspiciousActivityReport suspiciousActivityReport,
        EnrichmentContext context,
        CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([suspiciousActivityReport], context, stoppingToken);

        return resources[0];
    }

    public async Task<SuspiciousActivityReportResourceDto[]> CreateAsync(
        IReadOnlyCollection<SuspiciousActivityReport> suspiciousActivityReports,
        EnrichmentContext context,
        CancellationToken stoppingToken)
    {
        if (suspiciousActivityReports.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            suspiciousActivityReports.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);

        var users = await relatedRecordLoader.LoadUsersAsync(
            suspiciousActivityReports.Select(x => (ApplicationUserId?)x.OpenedByUserId)
                .Concat(suspiciousActivityReports.Select(x => x.FiledByUserId))
                .Concat(suspiciousActivityReports.Select(x => x.DecidedByUserId)), context, stoppingToken);

        var filingApprovalGates = await LoadFilingApprovalGatesAsync(suspiciousActivityReports, stoppingToken);

        var now = timeProvider.GetUtcNow();

        return suspiciousActivityReports.Select(x => new SuspiciousActivityReportResourceDto
        {
            SuspiciousActivityReport = x.ToDto(now),
            RequireFilingApproval = filingApprovalGates.GetValueOrDefault(x.OrganizationId),
            Organization = organizations.GetValueOrDefault(x.OrganizationId),
            OpenedByUser = users.GetValueOrDefault(x.OpenedByUserId),
            FiledByUser = x.FiledByUserId.HasValue ? users.GetValueOrDefault(x.FiledByUserId.Value) : null,
            DecidedByUser = x.DecidedByUserId.HasValue ? users.GetValueOrDefault(x.DecidedByUserId.Value) : null
        }).ToArray();
    }

    private async Task<Dictionary<OrganizationId, bool>> LoadFilingApprovalGatesAsync(
        IReadOnlyCollection<SuspiciousActivityReport> suspiciousActivityReports,
        CancellationToken stoppingToken)
    {
        var organizationIds = suspiciousActivityReports.Select(x => x.OrganizationId).Distinct();

        var filingApprovalGates = new Dictionary<OrganizationId, bool>();

        foreach (var organizationId in organizationIds)
        {
            var suspiciousActivityReportPolicy = await suspiciousActivityReportService.FindPolicyAsync(organizationId, stoppingToken);

            filingApprovalGates[organizationId] = FilingDeadlineResolver.RequiresFilingApproval(suspiciousActivityReportPolicy);
        }

        return filingApprovalGates;
    }
}
