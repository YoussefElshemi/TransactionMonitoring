using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.OrganizationUsers;

public record SetUserSubOrganizationRequestDto : OrganizationUserRequestDto
{
    [FromBody]
    public required SetUserSubOrganizationRequestBodyDto Body { get; init; }
}

public record SetUserSubOrganizationRequestBodyDto
{
    public Guid? SubOrganizationId { get; init; }
}
