using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Cases;

public record ChangeCaseStatusRequest
{
    public required CaseId CaseId { get; init; }
    public required CaseStatus Status { get; init; }
    public required Message? Note { get; init; }
    public required ApplicationUserId ChangedByUserId { get; init; }
}
