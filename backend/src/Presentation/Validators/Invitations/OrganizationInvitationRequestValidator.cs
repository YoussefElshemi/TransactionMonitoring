using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Invitations;

namespace Presentation.Validators.Invitations;

public class OrganizationInvitationRequestValidator : AbstractValidator<OrganizationInvitationRequestDto>
{
    public OrganizationInvitationRequestValidator(IOrganizationService organizationService, IInvitationService invitationService)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.InvitationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await invitationService.ExistsAsync(new OrganizationId(x.OrganizationId), new InvitationId(x.InvitationId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Invitation not found");
    }
}
