using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Parties;

public record PartyFacts
{
    public required PartyId PartyId { get; init; }
    public required PartyRole Role { get; init; }
    public required PartyIdentifier Identifier { get; init; }
    public required RiskScore? RiskScore { get; init; }
    public required FirstSeenAt FirstSeenAt { get; init; }
    public required LastSeenAt LastSeenAt { get; init; }

    public object? Read(PartyAttribute attribute, DateTimeOffset now)
    {
        return attribute switch
        {
            PartyAttribute.RiskScore => RiskScore.HasValue ? (decimal)(int)RiskScore.Value : null,
            PartyAttribute.DaysSinceFirstSeen => (decimal)(now - FirstSeenAt).TotalDays,
            PartyAttribute.DaysSinceLastSeen => (decimal)(now - LastSeenAt).TotalDays,
            _ => null
        };
    }
}
