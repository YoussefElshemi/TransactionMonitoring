using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Mappers.Webhooks;
using Core.Models.Common;
using Core.Models.OutboxMessages;
using Core.Models.WebhookConfigurations;
using Core.Models.Webhooks;
using Core.ValueObjects;

namespace Core.Services;

public class WebhookConfigurationService(
    IWebhookConfigurationRepository webhookConfigurationRepository,
    IWebhookService webhookService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : IWebhookConfigurationService
{
    public async Task<WebhookConfiguration> CreateAsync(CreateWebhookConfigurationRequest createWebhookConfigurationRequest, CancellationToken stoppingToken)
    {
        var webhookConfiguration = createWebhookConfigurationRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await webhookConfigurationRepository.AddAsync(webhookConfiguration, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(webhookConfiguration.WebhookConfigurationId, stoppingToken);
    }

    public async Task UpdateAsync(UpdateWebhookConfigurationRequest updateWebhookConfigurationRequest, CancellationToken stoppingToken)
    {
        await webhookConfigurationRepository.UpdateAsync(updateWebhookConfigurationRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, WebhookConfigurationId webhookConfigurationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return webhookConfigurationRepository.ExistsAsync(organizationId, webhookConfigurationId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<WebhookConfiguration>> ListAsync(ListOrganizationWebhookConfigurationRequest listOrganizationWebhookConfigurationRequest, CancellationToken stoppingToken)
    {
        return webhookConfigurationRepository.ListAsync(listOrganizationWebhookConfigurationRequest, stoppingToken);
    }

    public Task<WebhookConfiguration> GetAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken)
    {
        return webhookConfigurationRepository.GetAsync(webhookConfigurationId, stoppingToken);
    }

    public Task<List<WebhookConfiguration>> GetAllAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        return webhookConfigurationRepository.GetAllAsync(organizationId, stoppingToken);
    }

    public async Task<Result> TestAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken)
    {
        var webhookConfiguration = await GetAsync(webhookConfigurationId, stoppingToken);

        var payload = WebhookOutboxMapper.Render(
            new WebhookEvent(WebhookEvents.Test),
            webhookConfiguration.PayloadVersion,
            new TestWebhookMessage { TestId = Guid.NewGuid() },
            timeProvider.GetUtcNow());

        var outboxMessage = new CreateOutboxMessageRequest
        {
            OrganizationId = webhookConfiguration.OrganizationId,
            Payload = payload,
            Channel = NotificationChannel.Webhook,
            Destination = new Destination(webhookConfiguration.EndpointUrl.ToString()),
            SecretKey = webhookConfiguration.SecretKey,
            WebhookConfigurationId = webhookConfiguration.WebhookConfigurationId,
            Event = new WebhookEvent(WebhookEvents.Test),
            PayloadVersion = webhookConfiguration.PayloadVersion
        }.ToModel(timeProvider, modeContext.CurrentOrLive);

        var deliveryResult = await webhookService.SendAsync(outboxMessage, stoppingToken);

        return deliveryResult.Result;
    }

    public async Task<WebhookConfiguration> RotateSecretAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken)
    {
        await webhookConfigurationRepository.RotateSecretAsync(webhookConfigurationId, SecretKeyHelper.Generate(), stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(webhookConfigurationId, stoppingToken);
    }

    public async Task DeleteAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken)
    {
        await webhookConfigurationRepository.DeleteAsync(webhookConfigurationId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken)
    {
        await webhookConfigurationRepository.RecoverAsync(webhookConfigurationId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListOrganizationWebhookConfigurationRequest listOrganizationWebhookConfigurationRequest, CancellationToken stoppingToken)
    {
        return webhookConfigurationRepository.GetCountAsync(listOrganizationWebhookConfigurationRequest, stoppingToken);
    }
}