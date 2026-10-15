using Core.Enums;
using Core.Extensions;
using Core.Interfaces.Repositories;
using Core.Models.Alerts;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AlertRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListAlertRequest, AlertEntity, Alert>, IAlertRepository
{
    public async Task AddRangeAsync(List<Alert> alerts, CancellationToken stoppingToken)
    {
        var alertEntities = alerts.ConvertAll(x => x.ToEntity());
        await context.Alerts.AddRangeAsync(alertEntities, stoppingToken);
    }

    public async Task<Alert> GetAsync(AlertId alertId, CancellationToken stoppingToken)
    {
        var alertEntity = await Detail()
            .FirstAsync(x => x.AlertId == alertId && !x.IsDeleted, stoppingToken);

        return alertEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, AlertId alertId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.Alerts
            .AnyAsync(x => x.AlertId == alertId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<Alert?> FindDuplicateAsync(FindDuplicateAlertRequest findDuplicateAlertRequest, CancellationToken stoppingToken)
    {
        var alertEntity = await context.Alerts
            .Where(x => x.OrganizationId == findDuplicateAlertRequest.OrganizationId &&
                        x.PartyId == findDuplicateAlertRequest.PartyId &&
                        x.RuleId == findDuplicateAlertRequest.RuleId &&
                        x.StatusId == (int)AlertStatus.Open &&
                        x.RaisedAt >= findDuplicateAlertRequest.RaisedAfter &&
                        !x.IsDeleted &&
                        x.CaseId.HasValue &&
                        !CaseStatusExtensions.TerminalStatusIds.Contains(x.Case!.StatusId) &&
                        !x.Case.IsDeleted)
            .OrderByDescending(x => x.RaisedAt)
            .FirstOrDefaultAsync(stoppingToken);

        return alertEntity?.ToModel();
    }

    public async Task<CaseId?> FindGroupingCaseAsync(FindGroupingCaseRequest findGroupingCaseRequest, CancellationToken stoppingToken)
    {
        var queryable = context.Alerts
            .Where(x => x.OrganizationId == findGroupingCaseRequest.OrganizationId &&
                        x.PartyId == findGroupingCaseRequest.PartyId &&
                        x.CaseId.HasValue &&
                        x.RaisedAt >= findGroupingCaseRequest.RaisedAfter &&
                        !x.IsDeleted &&
                        !CaseStatusExtensions.TerminalStatusIds.Contains(x.Case!.StatusId) &&
                        !x.Case.IsDeleted);

        if (findGroupingCaseRequest.Scope is CaseGroupingScope.PartyAndRule)
        {
            queryable = queryable.Where(x => x.RuleId == findGroupingCaseRequest.RuleId);
        }

        var severity = (int)findGroupingCaseRequest.Severity;

        queryable = findGroupingCaseRequest.SeverityPolicy switch
        {
            CaseGroupingSeverityPolicy.AtLeastAsSevere => queryable.Where(x => x.Case!.SeverityId >= severity),
            CaseGroupingSeverityPolicy.SameSeverity => queryable.Where(x => x.Case!.SeverityId == severity),
            _ => queryable
        };

        if (findGroupingCaseRequest.MaxAlertsPerCase.HasValue)
        {
            var cap = (int)findGroupingCaseRequest.MaxAlertsPerCase.Value;

            queryable = queryable.Where(x => x.Case!.Alerts.Count(alert => !alert.IsDeleted) < cap);
        }

        var caseId = await queryable
            .OrderByDescending(x => x.RaisedAt)
            .Select(x => x.CaseId)
            .FirstOrDefaultAsync(stoppingToken);

        return caseId.HasValue ? new CaseId(caseId.Value) : null;
    }

    public async Task CloseByCaseAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var alertEntities = await context.Alerts
            .Where(x => x.CaseId == caseId && x.StatusId == (int)AlertStatus.Open && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        var now = timeProvider.GetUtcNow();

        foreach (var alertEntity in alertEntities)
        {
            alertEntity.StatusId = (int)AlertStatus.Closed;
            alertEntity.ClosedAt = now;
            alertEntity.UpdatedAt = now;

            context.Alerts.Update(alertEntity);
        }
    }

    public async Task ReopenByCaseAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var alertEntities = await context.Alerts
            .Where(x => x.CaseId == caseId && x.StatusId == (int)AlertStatus.Closed && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        var now = timeProvider.GetUtcNow();

        foreach (var alertEntity in alertEntities)
        {
            alertEntity.StatusId = (int)AlertStatus.Open;
            alertEntity.ClosedAt = null;
            alertEntity.UpdatedAt = now;

            context.Alerts.Update(alertEntity);
        }
    }

    public async Task<FriendlyName> GenerateFriendlyName(CancellationToken stoppingToken)
    {
        var result = await context.Database
            .SqlQueryRaw<int>($"SELECT nextval('\"{Schemas.Service}\".\"{Sequences.AlertNumber}\"') as \"Value\"")
            .SingleAsync(stoppingToken);

        return new FriendlyName("AL" + result.ToString("D10"));
    }

    protected override Task<List<Alert>> MapPageAsync(List<AlertEntity> items, ListAlertRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<AlertEntity> OrderByExpression()
    {
        return new OrderByExpression<AlertEntity, DateTimeOffset>(x => x.RaisedAt);
    }

    protected override IQueryable<AlertEntity> FilterByPagedRequest(ListAlertRequest listAlertRequest)
    {
        var queryable = context.Alerts
            .Where(x => x.OrganizationId == listAlertRequest.OrganizationId && !x.IsDeleted);

        if (listAlertRequest.FriendlyName.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.FriendlyName, $"%{listAlertRequest.FriendlyName}%"));
        }

        if (listAlertRequest.Status.HasValue)
        {
            queryable = queryable
                .Where(x => x.StatusId == (int)listAlertRequest.Status.Value);
        }

        if (listAlertRequest.Severity.HasValue)
        {
            queryable = queryable
                .Where(x => x.SeverityId == (int)listAlertRequest.Severity.Value);
        }

        if (listAlertRequest.RuleId.HasValue)
        {
            queryable = queryable
                .Where(x => x.RuleId == listAlertRequest.RuleId.Value);
        }

        if (listAlertRequest.PartyId.HasValue)
        {
            queryable = queryable
                .Where(x => x.PartyId == listAlertRequest.PartyId.Value);
        }

        if (listAlertRequest.CaseId.HasValue)
        {
            queryable = queryable
                .Where(x => x.CaseId == listAlertRequest.CaseId.Value);
        }

        return queryable;
    }

    protected override IQueryable<AlertEntity> FilterByDateRange(IQueryable<AlertEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.RaisedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.RaisedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }

    private IQueryable<AlertEntity> Detail()
    {
        return context.Alerts
            .Include(x => x.RuleEvaluation);
    }
}
