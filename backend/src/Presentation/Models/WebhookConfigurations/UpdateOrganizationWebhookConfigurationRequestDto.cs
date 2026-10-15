using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.WebhookConfigurations;

public record UpdateOrganizationWebhookConfigurationRequestDto : OrganizationWebhookConfigurationRequestDto
{
    [FromBody]
    public required UpdateWebhookConfigurationRequestBodyDto Body { get; init; }
}

public record UpdateWebhookConfigurationRequestBodyDto
{
    public string? EndpointUrl { get; init; }
    public bool? IsEnabled { get; init; }

    public string[]? Events { get; init; }

    public int? PayloadVersion { get; init; }
}
