using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.WebhookConfigurations;

public record CreateWebhookConfigurationRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required CreateWebhookConfigurationRequestBodyDto Body { get; init; }
}

public record CreateWebhookConfigurationRequestBodyDto
{
    public required string EndpointUrl { get; init; }
    public required bool IsEnabled { get; init; }

    public string[]? Events { get; init; }

    public int? PayloadVersion { get; init; }
}
