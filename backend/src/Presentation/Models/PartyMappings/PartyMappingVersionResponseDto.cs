namespace Presentation.Models.PartyMappings;

public record PartyMappingVersionResponseDto
{
    public required Guid PartyMappingVersionId { get; init; }
    public required Guid PartyMappingId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required int VersionNumber { get; init; }
    public required List<PartyRoleMappingDto> Mappings { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public required string Mode { get; init; }
}
