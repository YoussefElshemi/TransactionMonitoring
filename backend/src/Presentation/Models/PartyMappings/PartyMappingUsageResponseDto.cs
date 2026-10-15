namespace Presentation.Models.PartyMappings;

public record PartyMappingUsageResponseDto
{
    public required Guid RuleId { get; init; }
    public required string FriendlyName { get; init; }
    public required bool IsEnabled { get; init; }
    public required string[] Roles { get; init; }
}
