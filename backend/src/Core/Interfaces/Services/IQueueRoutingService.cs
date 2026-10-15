using Core.Models.Cases;
using Core.Models.Queues;

namespace Core.Interfaces.Services;

public interface IQueueRoutingService
{
    Task<QueueRouting> RouteAsync(RouteCaseRequest routeCaseRequest, CancellationToken stoppingToken);

    Task<Case?> ClaimNextAsync(ClaimNextRequest claimNextRequest, CancellationToken stoppingToken);
}
