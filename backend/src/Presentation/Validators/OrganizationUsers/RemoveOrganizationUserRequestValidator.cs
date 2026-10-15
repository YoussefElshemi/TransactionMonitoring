using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.OrganizationUsers;

namespace Presentation.Validators.OrganizationUsers;

public class RemoveOrganizationUserRequestValidator : AbstractValidator<RemoveOrganizationUserRequestDto>
{
    public RemoveOrganizationUserRequestValidator(
        IOrganizationService organizationService,
        IOrganizationUserService organizationUserService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationUserService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApplicationUserId(x.UserId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("User not found");
    }
}