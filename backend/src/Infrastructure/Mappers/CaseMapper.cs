using Core.Enums;
using Core.Models.Cases;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class CaseMapper
{
    public static Case ToModel(this CaseEntity caseEntity)
    {
        return new Case
        {
            Mode = (Mode)caseEntity.ModeId,
            CaseId = new CaseId(caseEntity.CaseId),
            FriendlyName = new FriendlyName(caseEntity.FriendlyName),
            OrganizationId = new OrganizationId(caseEntity.OrganizationId),
            TransactionId = caseEntity.TransactionId.HasValue ? new TransactionId(caseEntity.TransactionId.Value) : null,
            PartyId = caseEntity.PartyId.HasValue ? new PartyId(caseEntity.PartyId.Value) : null,
            Status = (CaseStatus)caseEntity.StatusId,
            Severity = (RuleSeverity)caseEntity.SeverityId,
            Disposition = caseEntity.DispositionId.HasValue ? (CaseDisposition)caseEntity.DispositionId.Value : null,
            CaseReasonCodeId = caseEntity.CaseReasonCodeId.HasValue ? new CaseReasonCodeId(caseEntity.CaseReasonCodeId.Value) : null,
            ReasonCode = caseEntity.ReasonCode?.ToModel(),
            QueueId = caseEntity.QueueId.HasValue ? new QueueId(caseEntity.QueueId.Value) : null,
            Queue = caseEntity.Queue?.ToModel(),
            AssignedToUserId = caseEntity.AssignedToUserId.HasValue ? new ApplicationUserId(caseEntity.AssignedToUserId.Value) : null,
            AssignedToUser = caseEntity.AssignedToUser?.ToModel(),
            RuleEvaluations = caseEntity.RuleEvaluations.Select(x => x.ToModel()).ToList(),
            Alerts = caseEntity.Alerts.Select(x => x.ToModel()).ToList(),
            AssignedAt = caseEntity.AssignedAt.HasValue ? new AssignedAt(caseEntity.AssignedAt.Value) : null,
            DueAt = new DueAt(caseEntity.DueAt),
            EscalatedAt = caseEntity.EscalatedAt.HasValue ? new EscalatedAt(caseEntity.EscalatedAt.Value) : null,
            EscalationRuleId = caseEntity.EscalationRuleId.HasValue ? new EscalationRuleId(caseEntity.EscalationRuleId.Value) : null,
            BreachNotifiedAt = caseEntity.BreachNotifiedAt.HasValue ? new BreachNotifiedAt(caseEntity.BreachNotifiedAt.Value) : null,
            ResolvedAt = caseEntity.ResolvedAt.HasValue ? new ResolvedAt(caseEntity.ResolvedAt.Value) : null,
            ReopenedAt = caseEntity.ReopenedAt.HasValue ? new ReopenedAt(caseEntity.ReopenedAt.Value) : null,
            CreatedAt = new CreatedAt(caseEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(caseEntity.UpdatedAt),
            IsDeleted = new IsDeleted(caseEntity.IsDeleted),
            DeletedAt = caseEntity.DeletedAt.HasValue ? new DeletedAt(caseEntity.DeletedAt.Value) : null
        };
    }

    public static CaseEntity ToEntity(this Case @case)
    {
        return new CaseEntity
        {
            ModeId = (int)@case.Mode,
            CaseId = @case.CaseId,
            FriendlyName = @case.FriendlyName,
            OrganizationId = @case.OrganizationId,
            TransactionId = @case.TransactionId,
            PartyId = @case.PartyId,
            StatusId = (int)@case.Status,
            SeverityId = (int)@case.Severity,
            DispositionId = @case.Disposition.HasValue ? (int)@case.Disposition.Value : null,
            CaseReasonCodeId = @case.CaseReasonCodeId,
            QueueId = @case.QueueId,
            AssignedToUserId = @case.AssignedToUserId,
            AssignedAt = @case.AssignedAt,
            DueAt = @case.DueAt,
            EscalatedAt = @case.EscalatedAt,
            EscalationRuleId = @case.EscalationRuleId,
            BreachNotifiedAt = @case.BreachNotifiedAt,
            ResolvedAt = @case.ResolvedAt,
            ReopenedAt = @case.ReopenedAt,
            CreatedAt = @case.CreatedAt,
            UpdatedAt = @case.UpdatedAt,
            IsDeleted = @case.IsDeleted,
            DeletedAt = @case.DeletedAt
        };
    }
}