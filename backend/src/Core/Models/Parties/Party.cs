using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Parties;

public record Party : BaseModel
{
    public required PartyId PartyId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required PartyIdentifier Identifier { get; init; }
    public required PartyDisplayName? DisplayName { get; init; }

    public required RiskScore? RiskScore { get; init; }
    public required RiskNote? RiskNote { get; init; }
    public required ApplicationUserId? RiskSetByUserId { get; init; }
    public required RiskSetAt? RiskSetAt { get; init; }

    public required FirstSeenAt FirstSeenAt { get; init; }
    public required LastSeenAt LastSeenAt { get; init; }

    public required List<PartyAttributeValue> Attributes { get; init; }

    public required List<PartyRole> Roles { get; init; }

    public required Mode Mode { get; init; }
}
