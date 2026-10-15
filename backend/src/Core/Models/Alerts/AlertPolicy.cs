using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Alerts;

public record AlertPolicy
{
    public required PartyRole SubjectRole { get; init; }
    public required DeduplicationWindow DeduplicationWindow { get; init; }
    public required CaseGroupingPeriod CaseGroupingPeriod { get; init; }
    public required IsEnabled CaseGroupingEnabled { get; init; }
    public required IsEnabled DeduplicationEnabled { get; init; }
    public required CaseGroupingScope CaseGroupingScope { get; init; }
    public required CaseGroupingSeverityPolicy CaseGroupingSeverityPolicy { get; init; }
    public required MaxAlertsPerCase? MaxAlertsPerCase { get; init; }
}
