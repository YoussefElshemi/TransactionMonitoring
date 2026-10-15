using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Cases;

public record SetCaseSlaTargetsRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required IReadOnlyDictionary<RuleSeverity, SlaTarget> Targets { get; init; }
}
