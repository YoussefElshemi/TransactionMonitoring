using Core.ValueObjects;

namespace Core.Models.Escalations;

public record ListEscalationCandidateRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required Count BatchSize { get; init; }

    // The case id breaks timestamp ties so a page boundary inside a tie skips nothing.
    public required CreatedAt? AfterCreatedAt { get; init; }
    public required CaseId? AfterCaseId { get; init; }
}
