namespace Presentation.Models.Status;

public record StatusSubscriptionResponseDto
{
    public required Guid StatusSubscriptionId { get; init; }
    public required string Message { get; init; }
}
