using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.OrganizationUsers;

namespace Presentation.Validators.OrganizationUsers;

public class UpdateOrganizationUserRequestValidator : AbstractValidator<UpdateOrganizationUserRequestDto>
{
    public UpdateOrganizationUserRequestValidator(IOrganizationService organizationService,
        IApplicationUserService applicationUserService)
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
            .MustAsync(async (x, _, ct) => await applicationUserService.ExistsAsync(new ApplicationUserId(x.UserId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("User not found");
    }
}