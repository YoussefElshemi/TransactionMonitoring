using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Invitations;

namespace Presentation.Validators.Invitations;

public class InvitationRequestValidator : AbstractValidator<InvitationRequestDto>
{
    public InvitationRequestValidator(IInvitationService invitationService)
    {
        RuleFor(x => x.InvitationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await invitationService.ExistsAsync(new OrganizationId(x.OrganizationId), new InvitationId(x.InvitationId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Invitation not found");
    }
}

public class ResendInvitationRequestValidator : AbstractValidator<ResendInvitationRequestDto>
{
    public ResendInvitationRequestValidator(IInvitationService invitationService)
    {
        RuleFor(x => x.InvitationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await invitationService.ExistsAsync(new OrganizationId(x.OrganizationId), new InvitationId(x.InvitationId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Invitation not found");
    }
}

public class RevokeInvitationRequestValidator : AbstractValidator<RevokeInvitationRequestDto>
{
    public RevokeInvitationRequestValidator(IInvitationService invitationService)
    {
        RuleFor(x => x.InvitationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await invitationService.ExistsAsync(new OrganizationId(x.OrganizationId), new InvitationId(x.InvitationId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Invitation not found");
    }
}
