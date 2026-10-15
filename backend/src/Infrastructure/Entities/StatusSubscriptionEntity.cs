using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record StatusSubscriptionEntity : BaseEntity
{
    public required Guid StatusSubscriptionId { get; init; }
    public required string Email { get; init; }
    public required string NormalizedEmail { get; init; }

    public required string ConfirmationTokenHash { get; set; }
    public required string UnsubscribeTokenHash { get; init; }

    public required DateTimeOffset? ConfirmedAt { get; set; }
    public required DateTimeOffset? UnsubscribedAt { get; set; }
}
