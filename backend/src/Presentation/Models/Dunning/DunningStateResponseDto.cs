namespace Presentation.Models.Dunning;

public record DunningStateResponseDto
{
    public required Guid DunningStateId { get; init; }
    public required Guid InvoiceId { get; init; }
    public required string? InvoiceNumber { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Stage { get; init; }

    public required decimal OutstandingAmount { get; init; }
    public required string? Currency { get; init; }

    public required DateTimeOffset? DueAt { get; init; }
    public required DateTimeOffset NextAttemptAt { get; init; }
    public required DateTimeOffset? LastNoticedAt { get; init; }
    public required int CollectionAttempts { get; init; }
    public required DateTimeOffset? LastAttemptedAt { get; init; }

    public required DateTimeOffset? RestrictionProposedAt { get; init; }

    public required DateTimeOffset? ResolvedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
