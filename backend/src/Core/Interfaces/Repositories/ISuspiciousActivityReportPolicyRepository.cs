using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ISuspiciousActivityReportPolicyRepository
{
    Task<SuspiciousActivityReportPolicy?> FindAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task SetAsync(SetSuspiciousActivityReportPolicyRequest setSuspiciousActivityReportPolicyRequest, CancellationToken stoppingToken);
}
