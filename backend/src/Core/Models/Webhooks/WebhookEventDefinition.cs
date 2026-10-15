using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Webhooks;

public record WebhookEventDefinition
{
    public required WebhookEvent Event { get; init; }
    public required WebhookEventCategory Category { get; init; }
    public required EventDescription Description { get; init; }

    public required IsSubscribable IsSubscribable { get; init; }

    public required WebhookPayloadVersion IntroducedInVersion { get; init; }
}
