using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.OrganizationWebhookConfigurations;
using Presentation.Models.WebhookConfigurations;
using Presentation.Validators.Common;

namespace Presentation.Validators.OrganizationWebhooks;

public class ListMyOrganizationWebhookConfigurationRequestValidator(
    IWebhookConfigurationService webhookConfigurationService,
    TimeProvider timeProvider)
    : PagedRequestValidator<ListMyOrganizationWebhookConfigurationRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListMyOrganizationWebhookConfigurationRequestDto request, CancellationToken stoppingToken)
    {
        return webhookConfigurationService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
