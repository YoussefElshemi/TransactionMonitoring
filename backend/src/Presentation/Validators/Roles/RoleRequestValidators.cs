using System.Net;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Roles;

namespace Presentation.Validators.Roles;

public class OrganizationRoleRequestValidator : AbstractValidator<OrganizationRoleRequestDto>
{
    public OrganizationRoleRequestValidator(IOrganizationService organizationService, IRoleService roleService)
    {
        this.AddRoleRules(organizationService, roleService, new IncludeDeleted(false));
    }
}

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequestDto>
{
    public UpdateRoleRequestValidator(IOrganizationService organizationService, IRoleService roleService)
    {
        this.AddRoleRules(organizationService, roleService, new IncludeDeleted(false));

        RuleFor(x => x.Body.Name)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(RoleName.MaxLength)
            .MustAsync(async (x, _, ct) =>
            {
                var existing = await roleService.FindByNameAsync(new OrganizationId(x.OrganizationId), new RoleName(x.Body.Name!), ct);

                return existing is null || existing.RoleId == x.RoleId;
            })
            .WithMessage("Role name is already declared for this organization")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Name));

        RuleFor(x => x.Body.Description)
            .MaximumLength(RoleDescription.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Description));

        RuleFor(x => x.Body.Permissions)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A role must carry at least one permission")
            .Must(x => x!.All(permission => PermissionHelper.TryParse(permission, out _)))
            .WithMessage("One or more permissions are not recognised")
            .When(x => x.Body.Permissions is not null);
    }
}

public class DeleteRoleRequestValidator : AbstractValidator<DeleteRoleRequestDto>
{
    public DeleteRoleRequestValidator(IOrganizationService organizationService, IRoleService roleService)
    {
        this.AddRoleRules(organizationService, roleService, new IncludeDeleted(false));
    }
}

public class RecoverRoleRequestValidator : AbstractValidator<RecoverRoleRequestDto>
{
    public RecoverRoleRequestValidator(IOrganizationService organizationService, IRoleService roleService)
    {
        this.AddRoleRules(organizationService, roleService, new IncludeDeleted(true));
    }
}

internal static class RoleRequestValidatorExtensions
{
    public static void AddRoleRules<T>(
        this AbstractValidator<T> validator,
        IOrganizationService organizationService,
        IRoleService roleService,
        IncludeDeleted includeDeleted)
        where T : OrganizationRoleRequestDto
    {
        validator.RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        validator.RuleFor(x => x.RoleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) =>
                await roleService.ExistsAsync(new OrganizationId(x.OrganizationId), new RoleId(x.RoleId), includeDeleted, ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Role not found");
    }
}
