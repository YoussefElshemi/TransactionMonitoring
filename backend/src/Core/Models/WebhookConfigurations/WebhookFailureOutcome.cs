using Core.ValueObjects;

namespace Core.Models.WebhookConfigurations;

public record WebhookFailureOutcome
{
    public required ConsecutiveFailures ConsecutiveFailures { get; init; }

    public required WasDisabled WasDisabled { get; init; }
}
