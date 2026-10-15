using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Common;
using Core.Models.OutboxMessages;
using Core.Models.Sandbox;
using Core.ValueObjects;

namespace Core.Services;

public class SandboxService(
    ISandboxRepository sandboxRepository,
    IWebhookDeliveryService webhookDeliveryService,
    ISimulationService simulationService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : ISandboxService
{
    public async Task<SandboxReset> ResetAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        EnforceTestMode("Resetting the sandbox");

        if (await simulationService.HasRunningAsync(organizationId, stoppingToken))
        {
            throw new SandboxBusyException();
        }

        var counts = await unitOfWork.ExecuteInTransactionAsync(
            () => sandboxRepository.ResetAsync(organizationId, stoppingToken), stoppingToken);

        return new SandboxReset
        {
            OrganizationId = organizationId,
            Mode = Mode.Test,
            Counts = counts,
            TotalDeleted = new Count(counts.Sum(x => x.Count)),
            ResetAt = new ResetAt(timeProvider.GetUtcNow())
        };
    }

    public Task<OutboxMessage> ReplayDeliveryAsync(OutboxMessageId outboxMessageId, CancellationToken stoppingToken)
    {
        EnforceTestMode("Replaying a webhook delivery");

        return webhookDeliveryService.ReplayAsync(outboxMessageId, stoppingToken);
    }

    public Task<Exists> DeliveryExistsAsync(OrganizationId organizationId, OutboxMessageId outboxMessageId, CancellationToken stoppingToken)
    {
        return webhookDeliveryService.ExistsAsync(organizationId, outboxMessageId, stoppingToken);
    }

    public Task<PagedResponse<OutboxMessage>> ListAsync(ListWebhookDeliveryRequest listWebhookDeliveryRequest, CancellationToken stoppingToken)
    {
        EnforceTestMode("Listing sandbox webhook deliveries");

        return webhookDeliveryService.ListAsync(listWebhookDeliveryRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListWebhookDeliveryRequest listWebhookDeliveryRequest, CancellationToken stoppingToken)
    {
        EnforceTestMode("Counting sandbox webhook deliveries");

        return webhookDeliveryService.GetCountAsync(listWebhookDeliveryRequest, stoppingToken);
    }

    private void EnforceTestMode(string action)
    {
        if (modeContext.CurrentOrLive != Mode.Test)
        {
            throw new NotAvailableInLiveModeException(action);
        }
    }
}
