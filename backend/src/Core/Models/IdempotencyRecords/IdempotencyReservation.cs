using Core.Enums;

namespace Core.Models.IdempotencyRecords;

public record IdempotencyReservation
{
    public required IdempotencyOutcome Outcome { get; init; }

    public required IdempotencyRecord? Record { get; init; }
}
