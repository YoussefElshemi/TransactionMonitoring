namespace Presentation.Models.PartyMappings;

public record PartyMappingResponseDto
{
    public required Guid PartyMappingId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required List<PartyRoleMappingDto> Mappings { get; init; }
    public required int CurrentVersionNumber { get; init; }
    public required bool IsDeleted { get; init; }
    public required DateTimeOffset? DeletedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
