namespace Presentation.Models.NotificationSubscriptions;

public class NotificationSubscriptionResponseDto
{
    public required Guid NotificationSubscriptionId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Channel { get; init; }
    public required string Destination { get; init; }
    public required string? MinimumSeverity { get; init; }
    public required bool NotifyOnBreach { get; init; }
    public required bool IsEnabled { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }

    public required string? Locale { get; init; }
}
