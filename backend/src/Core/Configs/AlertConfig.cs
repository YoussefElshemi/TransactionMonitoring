using Core.Enums;

namespace Core.Configs;

public record AlertConfig
{
    public int DefaultDeduplicationWindowMinutes { get; init; } = 60;
    public int DefaultCaseGroupingPeriodMinutes { get; init; } = 1440;
    public PartyRole DefaultSubjectRole { get; init; } = PartyRole.Remitter;

    public bool DefaultCaseGroupingEnabled { get; init; }

    public bool DefaultDeduplicationEnabled { get; init; } = true;

    public CaseGroupingScope DefaultCaseGroupingScope { get; init; } = CaseGroupingScope.Party;
    public CaseGroupingSeverityPolicy DefaultCaseGroupingSeverityPolicy { get; init; } = CaseGroupingSeverityPolicy.Any;
}
