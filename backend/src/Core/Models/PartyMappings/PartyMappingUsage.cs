using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.PartyMappings;

public record PartyMappingUsage
{
    public required RuleId RuleId { get; init; }
    public required FriendlyName FriendlyName { get; init; }
    public required IsEnabled IsEnabled { get; init; }
    public required List<PartyRole> Roles { get; init; }
}
