using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Organizations;

namespace Presentation.Models.SubOrganizations;

public record CreateSubOrganizationRequestDto : OrganizationRequestDto
{
    [FromBody]
    public required CreateSubOrganizationRequestBodyDto Body { get; init; }
}

public record CreateSubOrganizationRequestBodyDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }

    public string? ExternalReference { get; init; }

    public required bool IsEnabled { get; init; }
}
