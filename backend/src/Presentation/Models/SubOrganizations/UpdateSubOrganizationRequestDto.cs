using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.SubOrganizations;

public record UpdateSubOrganizationRequestDto : SubOrganizationRequestDto
{
    [FromBody]
    public required UpdateSubOrganizationRequestBodyDto Body { get; init; }
}

public record UpdateSubOrganizationRequestBodyDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ExternalReference { get; init; }
    public bool? ClearExternalReference { get; init; }
    public bool? IsEnabled { get; init; }
}
