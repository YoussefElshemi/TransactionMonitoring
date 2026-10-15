using System.Net;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Roles;

namespace Presentation.Validators.Roles;

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequestDto>
{
    public CreateRoleRequestValidator(IOrganizationService organizationService, IRoleService roleService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(RoleName.MaxLength)
            .MustAsync(async (x, _, ct) =>
            {
                var existing = await roleService.FindByNameAsync(new OrganizationId(x.OrganizationId), new RoleName(x.Body.Name), ct);

                return existing is null;
            })
            .WithMessage("Role name is already declared for this organization")

            .Must(x => !BuiltInRoleNames.Contains(x))
            .WithMessage("Role name is reserved for a built-in role");

        RuleFor(x => x.Body.Description)
            .MaximumLength(RoleDescription.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Description));

        RuleFor(x => x.Body.Permissions)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A role must carry at least one permission")
            .Must(x => x.All(permission => PermissionHelper.TryParse(permission, out _)))
            .WithMessage("One or more permissions are not recognised");
    }

    private static readonly HashSet<string> BuiltInRoleNames =
        new(Enum.GetNames<UserRole>(), StringComparer.OrdinalIgnoreCase);
}
