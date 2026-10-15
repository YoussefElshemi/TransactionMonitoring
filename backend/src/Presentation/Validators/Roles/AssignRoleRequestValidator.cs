using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Roles;

namespace Presentation.Validators.Roles;

public class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequestDto>
{
    public AssignRoleRequestValidator(
        IOrganizationService organizationService,
        IOrganizationUserService organizationUserService,
        IRoleService roleService,
        ISubOrganizationService subOrganizationService,
        IRuleSetService ruleSetService,
        IQueueService queueService)
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

        RuleFor(x => x.Body.RoleId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await roleService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new RoleId(x.Body.RoleId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Role not found");

        RuleFor(x => x.Body.ScopeType)
            .IsEnumName(typeof(PermissionScopeType), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.ScopeType));

        RuleFor(x => x.Body)
            .Must(x => x.SubOrganizationId.HasValue)
            .WithMessage("A sub-organization scope must name a sub-organization")
            .When(x => IsScope(x.Body.ScopeType, PermissionScopeType.SubOrganization));

        RuleFor(x => x.Body)
            .Must(x => x.RuleSetId.HasValue)
            .WithMessage("A rule set scope must name a rule set")
            .When(x => IsScope(x.Body.ScopeType, PermissionScopeType.RuleSet));

        RuleFor(x => x.Body)
            .Must(x => x.QueueId.HasValue)
            .WithMessage("A queue scope must name a queue")
            .When(x => IsScope(x.Body.ScopeType, PermissionScopeType.Queue));

        RuleFor(x => x.Body.SubOrganizationId)
            .MustAsync(async (x, _, ct) => await subOrganizationService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new SubOrganizationId(x.Body.SubOrganizationId!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Sub-organization not found")
            .When(x => x.Body.SubOrganizationId.HasValue);

        RuleFor(x => x.Body.RuleSetId)
            .MustAsync(async (x, _, ct) => await ruleSetService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new RuleSetId(x.Body.RuleSetId!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Rule set not found")
            .When(x => x.Body.RuleSetId.HasValue);

        RuleFor(x => x.Body.QueueId)
            .MustAsync(async (x, _, ct) => await queueService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new QueueId(x.Body.QueueId!.Value), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Queue not found")
            .When(x => x.Body.QueueId.HasValue);
    }

    private static bool IsScope(string? scopeType, PermissionScopeType expected)
    {
        return Enum.TryParse<PermissionScopeType>(scopeType, true, out var parsed) && parsed == expected;
    }
}

public class RevokeRoleRequestValidator : AbstractValidator<RevokeRoleRequestDto>
{
    public RevokeRoleRequestValidator(IOrganizationService organizationService, IRoleAssignmentService roleAssignmentService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.RoleAssignmentId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await roleAssignmentService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new RoleAssignmentId(x.RoleAssignmentId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Role assignment not found");
    }
}
