namespace Presentation.Models.Parties;

public record PartyResponseDto
{
    public required Guid PartyId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Identifier { get; init; }
    public required string? DisplayName { get; init; }
    public required string[] Roles { get; init; }
    public required int? RiskScore { get; init; }
    public required string? RiskNote { get; init; }
    public required Guid? RiskSetByUserId { get; init; }
    public required DateTimeOffset? RiskSetAt { get; init; }
    public required DateTimeOffset FirstSeenAt { get; init; }
    public required DateTimeOffset LastSeenAt { get; init; }
    public required PartyAttributeResponseDto[] Attributes { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}

public record PartyAttributeResponseDto
{
    public required string Name { get; init; }
    public required string Value { get; init; }
}
