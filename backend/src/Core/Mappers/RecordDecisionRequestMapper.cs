using Core.Enums;
using Core.Helpers;
using Core.Models.Decisions;
using Core.ValueObjects;

namespace Core.Mappers;

public static class RecordDecisionRequestMapper
{
    public static Decision ToModel(this RecordDecisionRequest recordDecisionRequest, TimeProvider timeProvider, Mode mode)
    {
        var verdict = DecisionVerdictResolver.ForAction(
            recordDecisionRequest.Outcome.Action,
            recordDecisionRequest.AnyMatched,
            recordDecisionRequest.CaseId.HasValue);
        var now = timeProvider.GetUtcNow();
        var held = verdict.Status is DecisionStatus.Held;

        return new Decision
        {
            Mode = mode,
            DecisionId = new DecisionId(Guid.NewGuid()),
            OrganizationId = recordDecisionRequest.OrganizationId,
            TransactionId = recordDecisionRequest.TransactionId,
            ExternalReference = null,
            Status = verdict.Status,
            ReasonCode = verdict.ReasonCode,
            CaseId = recordDecisionRequest.CaseId,
            RuleEvaluations = [],
            ResolvedByUserId = null,
            ResolvedByUser = null,
            HeldAt = held ? new HeldAt(now) : null,
            ExpiresAt = held && recordDecisionRequest.HoldPolicy.HoldWindow.HasValue
                ? new ExpiresAt(now + recordDecisionRequest.HoldPolicy.HoldWindow.Value.ToTimeSpan())
                : null,
            HoldExpiryVerdict = held ? recordDecisionRequest.HoldPolicy.HoldExpiryVerdict : null,
            ResolvedAt = verdict.Status.IsTerminal() ? new ResolvedAt(now) : null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }
}
