using Core.Enums;
using Core.Models.Alerts;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class AlertMapper
{
    public static Alert ToModel(this AlertEntity alertEntity)
    {
        return new Alert
        {
            Mode = (Mode)alertEntity.ModeId,
            AlertId = new AlertId(alertEntity.AlertId),
            FriendlyName = new FriendlyName(alertEntity.FriendlyName),
            OrganizationId = new OrganizationId(alertEntity.OrganizationId),
            Status = (AlertStatus)alertEntity.StatusId,
            Severity = (RuleSeverity)alertEntity.SeverityId,
            RuleId = new RuleId(alertEntity.RuleId),
            RuleVersionId = new RuleVersionId(alertEntity.RuleVersionId),
            RuleEvaluationId = new RuleEvaluationId(alertEntity.RuleEvaluationId),
            RuleEvaluation = alertEntity.RuleEvaluation?.ToModel(),
            TransactionId = new TransactionId(alertEntity.TransactionId),
            PartyId = alertEntity.PartyId.HasValue ? new PartyId(alertEntity.PartyId.Value) : null,
            PartyRole = (PartyRole?)alertEntity.PartyRoleId,
            CaseId = alertEntity.CaseId.HasValue ? new CaseId(alertEntity.CaseId.Value) : null,
            SuppressionReason = (AlertSuppressionReason?)alertEntity.SuppressionReasonId,
            SuppressedByAlertId = alertEntity.SuppressedByAlertId.HasValue ? new AlertId(alertEntity.SuppressedByAlertId.Value) : null,
            RaisedAt = new RaisedAt(alertEntity.RaisedAt),
            ClosedAt = alertEntity.ClosedAt.HasValue ? new ClosedAt(alertEntity.ClosedAt.Value) : null,
            CreatedAt = new CreatedAt(alertEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(alertEntity.UpdatedAt),
            IsDeleted = new IsDeleted(alertEntity.IsDeleted),
            DeletedAt = alertEntity.DeletedAt.HasValue ? new DeletedAt(alertEntity.DeletedAt.Value) : null
        };
    }

    public static AlertEntity ToEntity(this Alert alert)
    {
        return new AlertEntity
        {
            ModeId = (int)alert.Mode,
            AlertId = alert.AlertId,
            FriendlyName = alert.FriendlyName,
            OrganizationId = alert.OrganizationId,
            StatusId = (int)alert.Status,
            SeverityId = (int)alert.Severity,
            RuleId = alert.RuleId,
            RuleVersionId = alert.RuleVersionId,
            RuleEvaluationId = alert.RuleEvaluationId,
            TransactionId = alert.TransactionId,
            PartyId = alert.PartyId,
            PartyRoleId = (int?)alert.PartyRole,
            CaseId = alert.CaseId,
            SuppressionReasonId = (int?)alert.SuppressionReason,
            SuppressedByAlertId = alert.SuppressedByAlertId,
            RaisedAt = alert.RaisedAt,
            ClosedAt = alert.ClosedAt,
            CreatedAt = alert.CreatedAt,
            UpdatedAt = alert.UpdatedAt,
            IsDeleted = alert.IsDeleted,
            DeletedAt = alert.DeletedAt
        };
    }
}
