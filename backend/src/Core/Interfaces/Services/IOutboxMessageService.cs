using Core.Models.OutboxMessages;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IOutboxMessageService
{
    Task<OutboxMessage> CreateAsync(CreateOutboxMessageRequest createOutboxMessageRequest, CancellationToken stoppingToken);
    Task CreateManyAsync(List<CreateOutboxMessageRequest> createOutboxMessageRequests, CancellationToken stoppingToken);
    Task StageManyAsync(List<CreateOutboxMessageRequest> createOutboxMessageRequests, CancellationToken stoppingToken);
    Task<OutboxMessage> GetAsync(OutboxMessageId outboxMessageId, CancellationToken stoppingToken);
    Task<List<OutboxMessage>> ListPendingAsync(CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OutboxMessageId outboxMessageId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateOutboxMessageRequest updateOutboxMessageRequest, CancellationToken stoppingToken);
    Task ProcessAsync(OutboxMessage outboxMessage, CancellationToken stoppingToken);
}