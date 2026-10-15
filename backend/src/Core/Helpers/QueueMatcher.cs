using Core.Enums;
using Core.Mappers;
using Core.Models.Queues;

namespace Core.Helpers;

public static class QueueMatcher
{
    public static Queue? FirstMatch(
        IReadOnlyCollection<Queue> queues,
        RouteCaseRequest routeCaseRequest,
        DateTimeOffset now)
    {
        return queues
            .Where(x => x.IsEnabled)
            .OrderBy(x => (int)x.Priority)
            .ThenBy(x => (DateTimeOffset)x.CreatedAt)
            .FirstOrDefault(x => Matches(x, routeCaseRequest, now));
    }

    public static bool Matches(
        Queue queue,
        RouteCaseRequest routeCaseRequest,
        DateTimeOffset now)
    {
        return MatchesSeverity(queue, routeCaseRequest) &&
               MatchesRules(queue, routeCaseRequest) &&
               MatchesPayload(queue, routeCaseRequest, now);
    }

    private static bool MatchesSeverity(Queue queue, RouteCaseRequest routeCaseRequest)
    {
        return !queue.MinimumSeverity.HasValue || routeCaseRequest.Severity >= queue.MinimumSeverity.Value;
    }

    private static bool MatchesRules(Queue queue, RouteCaseRequest routeCaseRequest)
    {
        return queue.RuleIds.Count == 0 || queue.RuleIds.Exists(routeCaseRequest.RuleIds.Contains);
    }

    private static bool MatchesPayload(
        Queue queue,
        RouteCaseRequest routeCaseRequest,
        DateTimeOffset now)
    {
        if (queue.Matcher is null)
        {
            return true;
        }

        return RuleHelper.Matches(
            queue.Matcher.ToModel(),
            routeCaseRequest.Payload,
            RuleMatchPolarity.MatchesWhenTrue,
            now,
            routeCaseRequest.Parties);
    }
}
