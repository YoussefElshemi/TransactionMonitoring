using Core.Models.Queues;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IQueueRepository : IPagedRepository<ListQueueRequest, Queue>
{
    Task AddAsync(Queue queue, CancellationToken stoppingToken);
    Task<Queue> GetAsync(QueueId queueId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Queue?> FindByNameAsync(OrganizationId organizationId, QueueName name, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, QueueId queueId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);

    Task<List<Queue>> GetRoutableAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<QueueUsage> GetUsageAsync(QueueId queueId, CancellationToken stoppingToken);
    Task<List<QueueStatistic>> GetStatisticsAsync(OrganizationId organizationId, QueueId? queueId, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateQueueRequest updateQueueRequest, CancellationToken stoppingToken);
    Task DeleteAsync(QueueId queueId, CancellationToken stoppingToken);
    Task RecoverAsync(QueueId queueId, CancellationToken stoppingToken);
}
