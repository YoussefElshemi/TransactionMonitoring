using Core.ValueObjects;

namespace Core.Models.Alerts;

public record AlertDraft
{
    public required List<Alert> Alerts { get; init; }

    public required CaseId? CaseId { get; init; }

    public required AllSuppressed AllSuppressed { get; init; }
}
