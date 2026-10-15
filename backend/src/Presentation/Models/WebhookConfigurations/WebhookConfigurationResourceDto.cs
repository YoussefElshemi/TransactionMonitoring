using System.Text.Json.Serialization;
using Presentation.Models.Organizations;

namespace Presentation.Models.WebhookConfigurations;

public record WebhookConfigurationResourceDto
{
    public required WebhookConfigurationResponseDto WebhookConfiguration { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}
