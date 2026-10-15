using Core.Models.Queues;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IQueueService : IPagedService<ListQueueRequest, Queue>
{
    Task<Queue> CreateAsync(CreateQueueRequest createQueueRequest, CancellationToken stoppingToken);
    Task<Queue> GetAsync(QueueId queueId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Queue?> FindByNameAsync(OrganizationId organizationId, QueueName name, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, QueueId queueId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<List<QueueMember>> ListMembersAsync(QueueId queueId, CancellationToken stoppingToken);
    Task<Exists> IsMemberAsync(QueueId queueId, ApplicationUserId userId, CancellationToken stoppingToken);
    Task SetMembersAsync(SetQueueMembersRequest setQueueMembersRequest, CancellationToken stoppingToken);
    Task<QueueUsage> GetUsageAsync(QueueId queueId, CancellationToken stoppingToken);
    Task<List<QueueStatistic>> GetStatisticsAsync(OrganizationId organizationId, QueueId? queueId, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateQueueRequest updateQueueRequest, CancellationToken stoppingToken);
    Task DeleteAsync(QueueId queueId, CancellationToken stoppingToken);
    Task RecoverAsync(QueueId queueId, CancellationToken stoppingToken);
}
