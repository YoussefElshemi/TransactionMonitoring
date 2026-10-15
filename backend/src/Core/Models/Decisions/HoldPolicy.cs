using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Decisions;

public record HoldPolicy
{
    public required HoldWindow? HoldWindow { get; init; }
    public required HoldExpiryVerdict HoldExpiryVerdict { get; init; }
}
