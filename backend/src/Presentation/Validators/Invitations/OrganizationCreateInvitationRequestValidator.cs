using System.Net;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Invitations;

namespace Presentation.Validators.Invitations;

public class OrganizationCreateInvitationRequestValidator : AbstractValidator<OrganizationCreateInvitationRequestDto>
{
    public OrganizationCreateInvitationRequestValidator(
        IOrganizationService organizationService,
        ISubOrganizationService subOrganizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();

        RuleFor(x => x.Body.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(x => RoleHelper.TryParse(x, out var userRole) && userRole != UserRole.Admin)
            .WithMessage($"Role must be one of {nameof(UserRole.User)}, {nameof(UserRole.Manager)} or {nameof(UserRole.Auditor)}.");

        RuleFor(x => x.Body.SubOrganizationId)
            .MustAsync(async (x, _, ct) => await subOrganizationService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SubOrganizationId(x.Body.SubOrganizationId!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Sub-organization not found")
            .When(x => x.Body.SubOrganizationId.HasValue);
    }
}
