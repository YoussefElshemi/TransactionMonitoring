using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Organizations;

public record Organization : DeletableModel
{
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName FriendlyName { get; init; }
    public required OrganizationStatus Status { get; init; }
    public required HoldWindow? HoldWindow { get; init; }
    public required HoldExpiryVerdict? HoldExpiryVerdict { get; init; }

    public required DeduplicationWindow? DeduplicationWindow { get; init; }
    public required CaseGroupingPeriod? CaseGroupingPeriod { get; init; }

    public required IsEnabled? CaseGroupingEnabled { get; init; }
    public required IsEnabled? DeduplicationEnabled { get; init; }
    public required CaseGroupingScope? CaseGroupingScope { get; init; }
    public required CaseGroupingSeverityPolicy? CaseGroupingSeverityPolicy { get; init; }

    public required MaxAlertsPerCase? MaxAlertsPerCase { get; init; }

    public required SandboxCredits? SandboxMonthlyCredits { get; init; }

    public required Locale? DefaultLocale { get; init; }
    public required DisplayTimeZone? DefaultTimeZone { get; init; }
}