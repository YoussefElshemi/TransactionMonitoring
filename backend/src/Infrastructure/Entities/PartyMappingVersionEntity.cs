namespace Infrastructure.Entities;

public record PartyMappingVersionEntity : IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid PartyMappingVersionId { get; init; }
    public required Guid PartyMappingId { get; init; }
    public PartyMappingEntity PartyMapping { get; init; } = null!;
    public required Guid OrganizationId { get; init; }
    public required int VersionNumber { get; init; }
    public required string Mappings { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
