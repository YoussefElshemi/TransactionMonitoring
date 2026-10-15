using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.WebhookConfigurations;

public record ListOrganizationWebhookConfigurationRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName? FriendlyName { get; init; }
}