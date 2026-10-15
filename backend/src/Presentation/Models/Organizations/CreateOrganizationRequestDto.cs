namespace Presentation.Models.Organizations;

public record CreateOrganizationRequestDto
{
    public required string FriendlyName { get; init; }
}