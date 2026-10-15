namespace Core.Models.Notifications;

public record NotificationMessage
{
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required Guid? CaseId { get; init; }
    public required string Event { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
