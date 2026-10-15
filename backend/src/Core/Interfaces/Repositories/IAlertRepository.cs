using Core.Models.Alerts;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IAlertRepository : IPagedRepository<ListAlertRequest, Alert>
{
    Task AddRangeAsync(List<Alert> alerts, CancellationToken stoppingToken);
    Task<Alert> GetAsync(AlertId alertId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, AlertId alertId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Alert?> FindDuplicateAsync(FindDuplicateAlertRequest findDuplicateAlertRequest, CancellationToken stoppingToken);
    Task<CaseId?> FindGroupingCaseAsync(FindGroupingCaseRequest findGroupingCaseRequest, CancellationToken stoppingToken);
    Task CloseByCaseAsync(CaseId caseId, CancellationToken stoppingToken);
    Task ReopenByCaseAsync(CaseId caseId, CancellationToken stoppingToken);
    Task<FriendlyName> GenerateFriendlyName(CancellationToken stoppingToken);
}
