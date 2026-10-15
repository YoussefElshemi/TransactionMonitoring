using Core.Models.Alerts;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IAlertService : IPagedService<ListAlertRequest, Alert>
{
    Task<AlertDraft> PrepareAsync(RaiseAlertsRequest raiseAlertsRequest, CancellationToken stoppingToken);
    Task<List<Alert>> RaiseAsync(IReadOnlyCollection<Alert> alerts, CaseId? caseId, CancellationToken stoppingToken);
    Task<Alert> GetAsync(AlertId alertId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, AlertId alertId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task CloseByCaseAsync(CaseId caseId, CancellationToken stoppingToken);
}
