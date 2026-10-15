using Core.Constants;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers.Webhooks;
using Core.Models.Alerts;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Services;

public class AlertService(
    IAlertRepository alertRepository,
    IWebhookConfigurationService webhookConfigurationService,
    IOutboxMessageService outboxMessageService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : IAlertService
{
    public async Task<AlertDraft> PrepareAsync(RaiseAlertsRequest raiseAlertsRequest, CancellationToken stoppingToken)
    {
        var candidates = raiseAlertsRequest.Candidates.OrderByDescending(x => x.Severity).ToList();
        var alerts = new List<Alert>();

        foreach (var candidate in candidates)
        {
            var duplicate = await FindDuplicateAsync(candidate, raiseAlertsRequest, stoppingToken);
            var friendlyName = await alertRepository.GenerateFriendlyName(stoppingToken);

            alerts.Add(ToAlert(candidate, duplicate, friendlyName, raiseAlertsRequest));
        }

        return new AlertDraft
        {
            Alerts = alerts,
            CaseId = await ResolveCaseAsync(alerts, raiseAlertsRequest, stoppingToken),
            AllSuppressed = new AllSuppressed(alerts.Count > 0 && alerts.TrueForAll(x => x.Status is AlertStatus.Suppressed))
        };
    }

    public async Task<List<Alert>> RaiseAsync(IReadOnlyCollection<Alert> alerts, CaseId? caseId, CancellationToken stoppingToken)
    {
        if (alerts.Count == 0)
        {
            return [];
        }

        var toRaise = alerts
            .Select(x => x.Status is AlertStatus.Open ? x with { CaseId = caseId } : x)
            .ToList();

        await alertRepository.AddRangeAsync(toRaise, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        var webhookConfigurations = await webhookConfigurationService.GetAllAsync(toRaise[0].OrganizationId, stoppingToken);

        foreach (var alert in toRaise)
        {
            var @event = alert.Status is AlertStatus.Suppressed ? WebhookEvents.AlertSuppressed : WebhookEvents.AlertRaised;

            await outboxMessageService.StageManyAsync(
                webhookConfigurations.ToOutboxMessageRequests(@event, alert.ToWebhook(), timeProvider.GetUtcNow()),
                stoppingToken);
        }

        return toRaise;
    }

    public Task<Alert> GetAsync(AlertId alertId, CancellationToken stoppingToken)
    {
        return alertRepository.GetAsync(alertId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, AlertId alertId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return alertRepository.ExistsAsync(organizationId, alertId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<Alert>> ListAsync(ListAlertRequest listAlertRequest, CancellationToken stoppingToken)
    {
        return alertRepository.ListAsync(listAlertRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListAlertRequest listAlertRequest, CancellationToken stoppingToken)
    {
        return alertRepository.GetCountAsync(listAlertRequest, stoppingToken);
    }

    public async Task CloseByCaseAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        await alertRepository.CloseByCaseAsync(caseId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    private Task<Alert?> FindDuplicateAsync(AlertCandidate candidate, RaiseAlertsRequest raiseAlertsRequest, CancellationToken stoppingToken)
    {
        if (!candidate.PartyId.HasValue || !candidate.AlertPolicy.DeduplicationEnabled)
        {
            return Task.FromResult<Alert?>(null);
        }

        return alertRepository.FindDuplicateAsync(new FindDuplicateAlertRequest
        {
            OrganizationId = raiseAlertsRequest.OrganizationId,
            PartyId = candidate.PartyId.Value,
            RuleId = candidate.RuleId,
            RaisedAfter = new RaisedAt((DateTimeOffset)raiseAlertsRequest.RaisedAt - candidate.AlertPolicy.DeduplicationWindow.ToTimeSpan())
        }, stoppingToken);
    }

    private async Task<CaseId?> ResolveCaseAsync(List<Alert> alerts, RaiseAlertsRequest raiseAlertsRequest, CancellationToken stoppingToken)
    {
        var suppressedInto = alerts.Find(x =>
            x.Status is AlertStatus.Suppressed &&
            x.CaseId.HasValue &&
            GroupingEnabledFor(x, raiseAlertsRequest));

        if (suppressedInto is not null)
        {
            return suppressedInto.CaseId;
        }

        foreach (var alert in alerts.Where(x => x.Status is AlertStatus.Open && x.PartyId.HasValue))
        {
            var candidate = raiseAlertsRequest.Candidates.Find(x => x.RuleId == alert.RuleId);

            if (candidate is null || !candidate.AlertPolicy.CaseGroupingEnabled)
            {
                continue;
            }

            var caseId = await alertRepository.FindGroupingCaseAsync(new FindGroupingCaseRequest
            {
                OrganizationId = raiseAlertsRequest.OrganizationId,
                PartyId = alert.PartyId!.Value,
                RaisedAfter = new RaisedAt((DateTimeOffset)raiseAlertsRequest.RaisedAt - candidate.AlertPolicy.CaseGroupingPeriod.ToTimeSpan()),
                RuleId = candidate.RuleId,
                Severity = candidate.Severity,
                Scope = candidate.AlertPolicy.CaseGroupingScope,
                SeverityPolicy = candidate.AlertPolicy.CaseGroupingSeverityPolicy,
                MaxAlertsPerCase = candidate.AlertPolicy.MaxAlertsPerCase
            }, stoppingToken);

            if (caseId.HasValue)
            {
                return caseId;
            }
        }

        return null;
    }

    private static bool GroupingEnabledFor(Alert alert, RaiseAlertsRequest raiseAlertsRequest)
    {
        var candidate = raiseAlertsRequest.Candidates.Find(x => x.RuleId == alert.RuleId);

        return candidate is not null && candidate.AlertPolicy.CaseGroupingEnabled;
    }

    private Alert ToAlert(AlertCandidate candidate, Alert? duplicate, FriendlyName friendlyName, RaiseAlertsRequest raiseAlertsRequest)
    {
        var suppressed = duplicate is not null;

        return new Alert
        {
            Mode = modeContext.CurrentOrLive,
            AlertId = new AlertId(Guid.NewGuid()),
            FriendlyName = friendlyName,
            OrganizationId = raiseAlertsRequest.OrganizationId,
            Status = suppressed ? AlertStatus.Suppressed : AlertStatus.Open,
            Severity = candidate.Severity,
            RuleId = candidate.RuleId,
            RuleVersionId = candidate.RuleVersionId,
            RuleEvaluationId = candidate.RuleEvaluationId,
            RuleEvaluation = null,
            TransactionId = raiseAlertsRequest.TransactionId,
            PartyId = candidate.PartyId,
            PartyRole = candidate.PartyRole,

            CaseId = suppressed ? duplicate!.CaseId : null,
            SuppressionReason = suppressed ? AlertSuppressionReason.DuplicateInWindow : null,
            SuppressedByAlertId = suppressed ? duplicate!.AlertId : null,
            RaisedAt = raiseAlertsRequest.RaisedAt,
            ClosedAt = suppressed ? new ClosedAt(raiseAlertsRequest.RaisedAt) : null,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
