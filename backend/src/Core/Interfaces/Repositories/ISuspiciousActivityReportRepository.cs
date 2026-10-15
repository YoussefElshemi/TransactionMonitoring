using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ISuspiciousActivityReportRepository : IPagedRepository<ListSuspiciousActivityReportRequest, SuspiciousActivityReport>
{
    Task AddAsync(SuspiciousActivityReport suspiciousActivityReport, CancellationToken stoppingToken);
    Task<SuspiciousActivityReport> GetAsync(SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken);
    Task<List<SuspiciousActivityReport>> ListByIdsAsync(IReadOnlyCollection<SuspiciousActivityReportId> suspiciousActivityReportIds, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken);

    Task<SuspiciousActivityReport?> FindByCaseAsync(CaseId caseId, CancellationToken stoppingToken);
    Task<SuspiciousActivityReport> UpdateNarrativeAsync(UpdateSuspiciousActivityReportRequest updateSuspiciousActivityReportRequest, CancellationToken stoppingToken);

    Task<SuspiciousActivityReport> TransitionAsync(SuspiciousActivityReportTransition suspiciousActivityReportTransition, CancellationToken stoppingToken);
    Task<FriendlyName> GenerateFriendlyName(CancellationToken stoppingToken);

    Task<List<SuspiciousActivityReport>> ListOverdueNotificationCandidatesAsync(Count batchSize, CancellationToken stoppingToken);
    Task MarkOverdueNotifiedAsync(List<SuspiciousActivityReportId> suspiciousActivityReportIds, CancellationToken stoppingToken);

    Task<NarrativeSource> GetNarrativeSourceAsync(CaseId caseId, Count noteLimit, CancellationToken stoppingToken);
}
