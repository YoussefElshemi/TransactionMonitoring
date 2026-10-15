using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Organizations;

namespace Presentation.Models.SubOrganizations;

public record SubOrganizationRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid SubOrganizationId { get; init; }
}

public record DeleteSubOrganizationRequestDto : SubOrganizationRequestDto;

public record RecoverSubOrganizationRequestDto : SubOrganizationRequestDto;
