using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Presentation.Models.Common;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Parties;

public record PartyRequestDto
{
    [FromRoute]
    public required Guid PartyId { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}

public record ListPartyRequestDto : PagedRequestDto
{
    public string? Identifier { get; init; }
    public string? DisplayName { get; init; }
    public string? Role { get; init; }
    public string? Query { get; init; }
}

public record ListPartyTransactionRequestDto : PagedRequestDto
{
    [FromRoute]
    public required Guid PartyId { get; init; }

    public string? Role { get; init; }
}

public record ListPartyCaseRequestDto : PagedRequestDto
{
    [FromRoute]
    public required Guid PartyId { get; init; }

    public string? Status { get; init; }
}
