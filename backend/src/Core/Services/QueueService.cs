using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Queues;
using Core.ValueObjects;

namespace Core.Services;

public class QueueService(
    IQueueRepository queueRepository,
    IQueueMemberRepository queueMemberRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : IQueueService
{
    public async Task<Queue> CreateAsync(CreateQueueRequest createQueueRequest, CancellationToken stoppingToken)
    {
        var queue = createQueueRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await queueRepository.AddAsync(queue, stoppingToken);

        await queueMemberRepository.SetAsync(new SetQueueMembersRequest
        {
            QueueId = queue.QueueId,
            UserIds = createQueueRequest.MemberUserIds
        }, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(queue.QueueId, new IncludeDeleted(false), stoppingToken);
    }

    public Task<Queue> GetAsync(QueueId queueId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return queueRepository.GetAsync(queueId, includeDeleted, stoppingToken);
    }

    public Task<Queue?> FindByNameAsync(OrganizationId organizationId, QueueName name, CancellationToken stoppingToken)
    {
        return queueRepository.FindByNameAsync(organizationId, name, stoppingToken);
    }

    public Task<PagedResponse<Queue>> ListAsync(ListQueueRequest listQueueRequest, CancellationToken stoppingToken)
    {
        return queueRepository.ListAsync(listQueueRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListQueueRequest listQueueRequest, CancellationToken stoppingToken)
    {
        return queueRepository.GetCountAsync(listQueueRequest, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, QueueId queueId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return queueRepository.ExistsAsync(organizationId, queueId, includeDeleted, stoppingToken);
    }

    public Task<List<QueueMember>> ListMembersAsync(QueueId queueId, CancellationToken stoppingToken)
    {
        return queueMemberRepository.ListAsync(queueId, stoppingToken);
    }

    public Task<Exists> IsMemberAsync(QueueId queueId, ApplicationUserId userId, CancellationToken stoppingToken)
    {
        return queueMemberRepository.ExistsAsync(queueId, userId, stoppingToken);
    }

    public async Task SetMembersAsync(SetQueueMembersRequest setQueueMembersRequest, CancellationToken stoppingToken)
    {
        await queueMemberRepository.SetAsync(setQueueMembersRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<QueueUsage> GetUsageAsync(QueueId queueId, CancellationToken stoppingToken)
    {
        return queueRepository.GetUsageAsync(queueId, stoppingToken);
    }

    public Task<List<QueueStatistic>> GetStatisticsAsync(OrganizationId organizationId, QueueId? queueId, CancellationToken stoppingToken)
    {
        return queueRepository.GetStatisticsAsync(organizationId, queueId, stoppingToken);
    }

    public async Task UpdateAsync(UpdateQueueRequest updateQueueRequest, CancellationToken stoppingToken)
    {
        await queueRepository.UpdateAsync(updateQueueRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task DeleteAsync(QueueId queueId, CancellationToken stoppingToken)
    {
        await queueRepository.DeleteAsync(queueId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(QueueId queueId, CancellationToken stoppingToken)
    {
        await queueRepository.RecoverAsync(queueId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }
}
