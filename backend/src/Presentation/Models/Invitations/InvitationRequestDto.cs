using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Presentation.Models.Common;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Invitations;

public record InvitationRequestDto
{
    [FromRoute]
    public required Guid InvitationId { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}

// Separate type so it gets its own validator.
public record ResendInvitationRequestDto : InvitationRequestDto;

public record RevokeInvitationRequestDto : InvitationRequestDto;

public record CreateInvitationRequestBodyDto
{
    public required string Email { get; init; }

    public required string Role { get; init; }

    public Guid? SubOrganizationId { get; init; }
}

public record CreateInvitationRequestDto : CreateInvitationRequestBodyDto
{
    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}

public record OrganizationCreateInvitationRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required CreateInvitationRequestBodyDto Body { get; init; }
}

public record ListInvitationRequestDto : PagedRequestDto
{
    public string? Email { get; init; }

    public bool IncludeResolved { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        set => base.OrganizationId = value;
    }
}

public record PreviewInvitationRequestDto
{
    [FromQuery]
    public required string Token { get; init; }
}

public record AcceptInvitationRequestDto
{
    public required string Token { get; init; }

    public string? UserName { get; init; }
    public string? Password { get; init; }
}
