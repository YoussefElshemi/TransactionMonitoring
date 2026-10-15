using Core.Models.Queues;
using Presentation.Mappers.Rules;
using Presentation.Models.Queues;

namespace Presentation.Mappers.Queues;

public static class QueueDtoMapper
{
    public static QueueResponseDto ToDto(this Queue queue)
    {
        return new QueueResponseDto
        {
            Mode = queue.Mode.ToString(),
            QueueId = queue.QueueId,
            OrganizationId = queue.OrganizationId,
            Name = queue.Name,
            Description = queue.Description?.ToString(),
            Priority = queue.Priority,
            RoutingPolicy = queue.RoutingPolicy.ToString(),
            MinimumSeverity = queue.MinimumSeverity?.ToString(),
            RuleIds = queue.RuleIds.Select(x => (Guid)x).ToArray(),
            Matcher = queue.Matcher?.ToDto(),
            IsEnabled = queue.IsEnabled,
            Members = queue.Members.Select(x => x.ToDto()).ToArray(),
            CreatedAt = queue.CreatedAt,
            UpdatedAt = queue.UpdatedAt
        };
    }

    public static QueueMemberResponseDto ToDto(this QueueMember queueMember)
    {
        return new QueueMemberResponseDto
        {
            UserId = queueMember.UserId,
            UserName = queueMember.User?.UserName?.ToString(),
            LastAssignedAt = queueMember.LastAssignedAt.HasValue ? (DateTimeOffset)queueMember.LastAssignedAt.Value : null
        };
    }

    public static QueueUsageResponseDto ToDto(this QueueUsage queueUsage)
    {
        return new QueueUsageResponseDto
        {
            QueueId = queueUsage.QueueId,
            Name = queueUsage.Name,
            CaseCount = queueUsage.CaseCount,
            OpenCaseCount = queueUsage.OpenCaseCount,
            MemberCount = queueUsage.MemberCount
        };
    }

    public static QueueStatisticResponseDto ToDto(this QueueStatistic queueStatistic)
    {
        return new QueueStatisticResponseDto
        {
            QueueId = queueStatistic.QueueId,
            Name = queueStatistic.Name,
            Priority = queueStatistic.Priority,
            RoutingPolicy = queueStatistic.RoutingPolicy.ToString(),
            IsEnabled = queueStatistic.IsEnabled,
            Depth = queueStatistic.Depth,
            InProgressCount = queueStatistic.InProgressCount,
            OldestWaitingForSeconds = queueStatistic.OldestWaitingFor?.TotalSeconds,
            MemberLoads = queueStatistic.MemberLoads.Select(x => x.ToDto()).ToArray()
        };
    }

    public static QueueMemberLoadResponseDto ToDto(this QueueMemberLoad queueMemberLoad)
    {
        return new QueueMemberLoadResponseDto
        {
            UserId = queueMemberLoad.UserId,
            UserName = queueMemberLoad.User?.UserName?.ToString(),
            OpenCaseCount = queueMemberLoad.OpenCaseCount,
            LastAssignedAt = queueMemberLoad.LastAssignedAt.HasValue ? (DateTimeOffset)queueMemberLoad.LastAssignedAt.Value : null
        };
    }
}
