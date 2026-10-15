using Core.Enums;
using Core.Models.Queues;
using Core.ValueObjects;

namespace Core.Helpers;

public static class QueueAssigneeResolver
{
    public static ApplicationUserId? Resolve(QueueRoutingPolicy routingPolicy, IReadOnlyCollection<QueueMemberLoad> memberLoads)
    {
        if (memberLoads.Count == 0)
        {
            return null;
        }

        return routingPolicy switch
        {
            QueueRoutingPolicy.RoundRobin => RoundRobin(memberLoads),
            QueueRoutingPolicy.LeastLoaded => LeastLoaded(memberLoads),
            _ => null
        };
    }

    private static ApplicationUserId RoundRobin(IReadOnlyCollection<QueueMemberLoad> memberLoads)
    {
        return memberLoads
            .OrderBy(x => x.LastAssignedAt.HasValue)
            .ThenBy(x => x.LastAssignedAt.HasValue ? x.LastAssignedAt.Value : DateTimeOffset.MinValue)
            .ThenBy(x => (Guid)x.UserId)
            .First()
            .UserId;
    }

    private static ApplicationUserId LeastLoaded(IReadOnlyCollection<QueueMemberLoad> memberLoads)
    {
        var lightest = memberLoads.Min(x => (int)x.OpenCaseCount);

        return RoundRobin(memberLoads.Where(x => x.OpenCaseCount == lightest).ToList());
    }
}
