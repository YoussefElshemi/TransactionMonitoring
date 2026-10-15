using Core.Models.Queues;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IQueueMemberRepository
{
    Task<List<QueueMember>> ListAsync(QueueId queueId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(QueueId queueId, ApplicationUserId userId, CancellationToken stoppingToken);

    Task<List<QueueMemberLoad>> GetLoadsAsync(QueueId queueId, CancellationToken stoppingToken);
    Task SetAsync(SetQueueMembersRequest setQueueMembersRequest, CancellationToken stoppingToken);
    Task TouchAsync(QueueId queueId, ApplicationUserId userId, CancellationToken stoppingToken);

    Task<Count> ReplaceMemberAsync(
        OrganizationId organizationId, ApplicationUserId fromUserId, ApplicationUserId? toUserId, CancellationToken stoppingToken);
}
