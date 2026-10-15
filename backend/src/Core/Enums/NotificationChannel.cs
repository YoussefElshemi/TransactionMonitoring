namespace Core.Enums;

// Webhook is numbered first so outbox rows written before channels existed still resolve.
public enum NotificationChannel
{
    Webhook = 1,
    Email,
    Chat
}
