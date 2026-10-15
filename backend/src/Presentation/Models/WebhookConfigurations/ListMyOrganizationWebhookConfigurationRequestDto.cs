using Presentation.Models.Common;

namespace Presentation.Models.WebhookConfigurations;

public record ListMyOrganizationWebhookConfigurationRequestDto : PagedRequestDto
{
    public string? FriendlyName { get; init; }
}
