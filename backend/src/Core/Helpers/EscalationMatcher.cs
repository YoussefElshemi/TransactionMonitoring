using Core.Enums;
using Core.Mappers;
using Core.Models.Escalations;
using Core.Models.Parties;

namespace Core.Helpers;

public static class EscalationMatcher
{
    public static EscalationRule? FirstMatch(
        IReadOnlyCollection<EscalationRule> escalationRules,
        EscalationCandidate escalationCandidate,
        IReadOnlyList<PartyFacts> parties,
        DateTimeOffset now)
    {
        return escalationRules
            .Where(x => x.IsEnabled)
            .OrderBy(x => (int)x.Priority)
            .ThenBy(x => (DateTimeOffset)x.CreatedAt)
            .FirstOrDefault(x => Matches(x, escalationCandidate, parties, now));
    }

    public static bool Matches(
        EscalationRule escalationRule,
        EscalationCandidate escalationCandidate,
        IReadOnlyList<PartyFacts> parties,
        DateTimeOffset now)
    {
        if (!HasCondition(escalationRule))
        {
            return false;
        }

        return MatchesUnassigned(escalationRule, escalationCandidate, now) &&
               MatchesHoldExpiry(escalationRule, escalationCandidate, now) &&
               MatchesSeverity(escalationRule, escalationCandidate) &&
               MatchesPayload(escalationRule, escalationCandidate, parties, now);
    }

    private static bool HasCondition(EscalationRule escalationRule)
    {
        return escalationRule.UnassignedFor.HasValue ||
               escalationRule.HoldExpiryWithin.HasValue ||
               escalationRule.MinimumSeverity.HasValue ||
               escalationRule.Matcher is not null;
    }

    private static bool MatchesUnassigned(EscalationRule escalationRule, EscalationCandidate escalationCandidate, DateTimeOffset now)
    {
        if (!escalationRule.UnassignedFor.HasValue)
        {
            return true;
        }

        return !escalationCandidate.Case.AssignedToUserId.HasValue &&
               now - escalationCandidate.Case.CreatedAt >= escalationRule.UnassignedFor.Value.ToTimeSpan();
    }

    private static bool MatchesHoldExpiry(EscalationRule escalationRule, EscalationCandidate escalationCandidate, DateTimeOffset now)
    {
        if (!escalationRule.HoldExpiryWithin.HasValue)
        {
            return true;
        }

        return escalationCandidate.HoldExpiresAt.HasValue &&
               escalationCandidate.HoldExpiresAt.Value - now <= escalationRule.HoldExpiryWithin.Value.ToTimeSpan();
    }

    private static bool MatchesSeverity(EscalationRule escalationRule, EscalationCandidate escalationCandidate)
    {
        return !escalationRule.MinimumSeverity.HasValue ||
               escalationCandidate.Case.Severity >= escalationRule.MinimumSeverity.Value;
    }

    private static bool MatchesPayload(
        EscalationRule escalationRule,
        EscalationCandidate escalationCandidate,
        IReadOnlyList<PartyFacts> parties,
        DateTimeOffset now)
    {
        if (escalationRule.Matcher is null)
        {
            return true;
        }

        return RuleHelper.Matches(
            escalationRule.Matcher.ToModel(),
            escalationCandidate.Payload,
            RuleMatchPolarity.MatchesWhenTrue,
            now,
            parties);
    }
}
