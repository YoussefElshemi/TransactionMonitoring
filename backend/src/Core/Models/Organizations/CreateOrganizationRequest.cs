using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Organizations;

public record CreateOrganizationRequest
{
    public required FriendlyName FriendlyName { get; init; }
    public required OrganizationStatus Status { get; init; }
    public required HoldWindow? HoldWindow { get; init; }
    public required HoldExpiryVerdict? HoldExpiryVerdict { get; init; }
    public required DeduplicationWindow? DeduplicationWindow { get; init; }
    public required CaseGroupingPeriod? CaseGroupingPeriod { get; init; }
}