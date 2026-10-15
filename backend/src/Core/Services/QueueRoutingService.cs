using Core.Constants;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers.Webhooks;
using Core.Models.Cases;
using Core.Models.Queues;

namespace Core.Services;

public class QueueRoutingService(
    IQueueRepository queueRepository,
    IQueueMemberRepository queueMemberRepository,
    ICaseRepository caseRepository,
    IWebhookConfigurationService webhookConfigurationService,
    IOutboxMessageService outboxMessageService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IQueueRoutingService
{
    public async Task<QueueRouting> RouteAsync(RouteCaseRequest routeCaseRequest, CancellationToken stoppingToken)
    {
        var queues = await queueRepository.GetRoutableAsync(routeCaseRequest.OrganizationId, stoppingToken);

        if (queues.Count == 0)
        {
            return Unrouted();
        }

        var queue = QueueMatcher.FirstMatch(queues, routeCaseRequest, timeProvider.GetUtcNow());

        if (queue is null)
        {
            return Unrouted();
        }

        var memberLoads = await queueMemberRepository.GetLoadsAsync(queue.QueueId, stoppingToken);
        var assignedToUserId = QueueAssigneeResolver.Resolve(queue.RoutingPolicy, memberLoads);

        if (assignedToUserId.HasValue)
        {
            await queueMemberRepository.TouchAsync(queue.QueueId, assignedToUserId.Value, stoppingToken);
        }

        return new QueueRouting
        {
            QueueId = queue.QueueId,
            AssignedToUserId = assignedToUserId
        };
    }

    public async Task<Case?> ClaimNextAsync(ClaimNextRequest claimNextRequest, CancellationToken stoppingToken)
    {
        var @case = await caseRepository.ClaimNextAsync(claimNextRequest, stoppingToken);

        if (@case is null)
        {
            return null;
        }

        // The claim is already committed; advancing the rotation afterwards only affects who is offered work next.
        await queueMemberRepository.TouchAsync(@case.QueueId!.Value, claimNextRequest.UserId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        var webhookConfigurations = await webhookConfigurationService.GetAllAsync(@case.OrganizationId, stoppingToken);

        await outboxMessageService.CreateManyAsync(
            webhookConfigurations.ToOutboxMessageRequests(WebhookEvents.CaseAssigned, @case.ToWebhook(), timeProvider.GetUtcNow()),
            stoppingToken);

        return @case;
    }

    private static QueueRouting Unrouted()
    {
        return new QueueRouting
        {
            QueueId = null,
            AssignedToUserId = null
        };
    }
}
