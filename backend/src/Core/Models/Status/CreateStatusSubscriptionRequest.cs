using Core.ValueObjects;

namespace Core.Models.Status;

public record CreateStatusSubscriptionRequest
{
    public required Email Email { get; init; }
}
