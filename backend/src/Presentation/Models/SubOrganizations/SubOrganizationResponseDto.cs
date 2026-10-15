namespace Presentation.Models.SubOrganizations;

public record SubOrganizationResponseDto
{
    public required Guid SubOrganizationId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required string? ExternalReference { get; init; }
    public required bool IsEnabled { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
