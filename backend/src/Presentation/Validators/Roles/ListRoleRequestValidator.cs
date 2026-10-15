using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Roles;
using Presentation.Models.Roles;
using Presentation.Validators.Common;

namespace Presentation.Validators.Roles;

public class ListRoleRequestValidator(IRoleService roleService, TimeProvider timeProvider)
    : PagedRequestValidator<OrganizationListRoleRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.Name)
            .MaximumLength(RoleName.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Name));
    }

    protected override Task<TotalCount> GetCountAsync(OrganizationListRoleRequestDto request, CancellationToken stoppingToken)
    {
        return roleService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}

public class ListRoleAssignmentRequestValidator(IRoleAssignmentService roleAssignmentService, TimeProvider timeProvider)
    : PagedRequestValidator<OrganizationListRoleAssignmentRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(OrganizationListRoleAssignmentRequestDto request, CancellationToken stoppingToken)
    {
        return roleAssignmentService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
