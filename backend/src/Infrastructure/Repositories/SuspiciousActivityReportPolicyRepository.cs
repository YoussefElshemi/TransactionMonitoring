using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SuspiciousActivityReportPolicyRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider,
    IModeContext modeContext) : ISuspiciousActivityReportPolicyRepository
{
    public async Task<SuspiciousActivityReportPolicy?> FindAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var suspiciousActivityReportPolicyEntity = await context.SuspiciousActivityReportPolicies
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId, stoppingToken);

        return suspiciousActivityReportPolicyEntity?.ToModel();
    }

    public async Task SetAsync(
        SetSuspiciousActivityReportPolicyRequest setSuspiciousActivityReportPolicyRequest,
        CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var reportingEntityId = setSuspiciousActivityReportPolicyRequest.ReportingEntityId?.ToString();
        var filingDeadlineDays = setSuspiciousActivityReportPolicyRequest.FilingDeadline.HasValue
            ? (int)setSuspiciousActivityReportPolicyRequest.FilingDeadline.Value
            : (int?)null;
        var amountPath = setSuspiciousActivityReportPolicyRequest.AmountPath?.ToString();
        var currencyPath = setSuspiciousActivityReportPolicyRequest.CurrencyPath?.ToString();

        var suspiciousActivityReportPolicyEntity = await context.SuspiciousActivityReportPolicies
            .FirstOrDefaultAsync(x => x.OrganizationId == setSuspiciousActivityReportPolicyRequest.OrganizationId, stoppingToken);

        if (suspiciousActivityReportPolicyEntity is null)
        {
            await context.SuspiciousActivityReportPolicies.AddAsync(new SuspiciousActivityReportPolicyEntity
            {
                ModeId = (int)modeContext.CurrentOrLive,
                OrganizationId = setSuspiciousActivityReportPolicyRequest.OrganizationId,
                ReportingEntityId = reportingEntityId,
                FilingDeadlineDays = filingDeadlineDays,
                RequireFilingApproval = setSuspiciousActivityReportPolicyRequest.RequireFilingApproval,
                AmountPath = amountPath,
                CurrencyPath = currencyPath,
                CreatedAt = now,
                UpdatedAt = now
            }, stoppingToken);

            return;
        }

        suspiciousActivityReportPolicyEntity.ReportingEntityId = reportingEntityId;
        suspiciousActivityReportPolicyEntity.FilingDeadlineDays = filingDeadlineDays;
        suspiciousActivityReportPolicyEntity.RequireFilingApproval = setSuspiciousActivityReportPolicyRequest.RequireFilingApproval;
        suspiciousActivityReportPolicyEntity.AmountPath = amountPath;
        suspiciousActivityReportPolicyEntity.CurrencyPath = currencyPath;
        suspiciousActivityReportPolicyEntity.UpdatedAt = now;

        context.SuspiciousActivityReportPolicies.Update(suspiciousActivityReportPolicyEntity);
    }
}
