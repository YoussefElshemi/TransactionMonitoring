using Core.ValueObjects;

namespace Core.Models.Cases;

public record ReopenCaseRequest
{
    public required CaseId CaseId { get; init; }
    public required Message Reason { get; init; }
    public required ApplicationUserId ReopenedByUserId { get; init; }
}
