using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Organizations;

public record UpdateOrganizationRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName? FriendlyName { get; init; }
    public required OrganizationStatus? Status { get; init; }
    public required Patch<HoldWindow?> HoldWindow { get; init; }
    public required Patch<HoldExpiryVerdict?> HoldExpiryVerdict { get; init; }

    public required Patch<DeduplicationWindow?> DeduplicationWindow { get; init; }
    public required Patch<CaseGroupingPeriod?> CaseGroupingPeriod { get; init; }
    public required Patch<IsEnabled?> CaseGroupingEnabled { get; init; }
    public required Patch<IsEnabled?> DeduplicationEnabled { get; init; }
    public required Patch<CaseGroupingScope?> CaseGroupingScope { get; init; }
    public required Patch<CaseGroupingSeverityPolicy?> CaseGroupingSeverityPolicy { get; init; }
    public required Patch<MaxAlertsPerCase?> MaxAlertsPerCase { get; init; }
    public required Patch<SandboxCredits?> SandboxMonthlyCredits { get; init; }
    public required Patch<Locale?> DefaultLocale { get; init; }
    public required Patch<DisplayTimeZone?> DefaultTimeZone { get; init; }
}