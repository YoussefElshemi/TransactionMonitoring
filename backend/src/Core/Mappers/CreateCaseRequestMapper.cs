using Core.Configs;
using Core.Enums;
using Core.Helpers;
using Core.Models.Cases;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateCaseRequestMapper
{
    public static Case ToModel(
        this CreateCaseRequest createCaseRequest,
        IReadOnlyDictionary<RuleSeverity, SlaTarget> slaTargets,
        CaseConfig caseConfig,
        TimeProvider timeProvider, Mode mode)
    {
        var now = timeProvider.GetUtcNow();

        return new Case
        {
            Mode = mode,
            CaseId = new CaseId(Guid.NewGuid()),
            Status = CaseStatus.Pending,
            Severity = createCaseRequest.Severity,
            OrganizationId = createCaseRequest.OrganizationId,
            TransactionId = createCaseRequest.TransactionId,
            PartyId = createCaseRequest.PartyId,
            RuleEvaluations = [],
            Alerts = [],
            Disposition = null,
            CaseReasonCodeId = null,
            ReasonCode = null,
            QueueId = null,
            Queue = null,
            FriendlyName = default,
            AssignedToUserId = null,
            AssignedToUser = null,
            AssignedAt = null,
            DueAt = CaseDueResolver.Resolve(
                createCaseRequest.Severity, createCaseRequest.HoldPolicy, slaTargets, caseConfig, now),
            EscalatedAt = null,
            EscalationRuleId = null,
            BreachNotifiedAt = null,
            ResolvedAt = null,
            ReopenedAt = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
