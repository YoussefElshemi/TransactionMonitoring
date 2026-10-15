using Core.ValueObjects;

namespace Core.Models.Cases;

public record EvidencePackActor
{
    public required ApplicationUserId UserId { get; init; }
    public required UserName? UserName { get; init; }
    public required Email? Email { get; init; }
}
