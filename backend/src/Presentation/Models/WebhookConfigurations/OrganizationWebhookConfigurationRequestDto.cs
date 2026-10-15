using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Organizations;

namespace Presentation.Models.WebhookConfigurations;

public record OrganizationWebhookConfigurationRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid WebhookConfigurationId { get; init; }
}