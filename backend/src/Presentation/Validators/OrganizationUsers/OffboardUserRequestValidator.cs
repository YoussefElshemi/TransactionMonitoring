using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.OrganizationUsers;

namespace Presentation.Validators.OrganizationUsers;

public class OffboardUserRequestValidator : AbstractValidator<OffboardUserRequestDto>
{
    public OffboardUserRequestValidator(
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
            .MustAsync(async (x, _, ct) => await organizationUserService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new ApplicationUserId(x.UserId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("User was not found in Organization");

        RuleFor(x => x.Body.SuccessorUserId)
            .MustAsync(async (x, _, ct) => await organizationUserService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new ApplicationUserId(x.Body.SuccessorUserId!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Successor was not found in Organization")
            .When(x => x.Body.SuccessorUserId.HasValue);
    }
}
