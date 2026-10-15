using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Cases;
using Core.Models.Organizations;
using Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Presentation.Models.Cases;

namespace Presentation.Validators.Cases;

public class AssignCaseRequestValidator : AbstractValidator<AssignCaseRequestDto>
{
    private readonly IApplicationUserService _applicationUserService;

    public AssignCaseRequestValidator(
        ICaseService caseService,
        IOrganizationUserService organizationUserService,
        IApplicationUserService applicationUserService)
    {
        _applicationUserService = applicationUserService;

        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found")
            .MustAsync(async (x, ct) =>
            {
                var @case = await caseService.GetAsync(new CaseId(x), ct);
                return @case.Status == CaseStatus.Pending;
            })
            .WithMessage($"{nameof(Case)} must be in a {nameof(CaseStatus.Pending)} status");

        RuleFor(x => x.Body.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEqual(Guid.Empty)
            .MustAsync(async (x, _, ct) => await organizationUserService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApplicationUserId(x.Body.UserId!.Value), new IncludeDeleted(false), ct))
            .WithMessage($"User was not found in {nameof(Organization)}")
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .CustomAsync(CanAssignToThisUser);
            });
    }

    private async Task CanAssignToThisUser(AssignCaseRequestDto assignCaseRequestDto, ValidationContext<AssignCaseRequestDto> context, CancellationToken stoppingToken)
    {
        if (assignCaseRequestDto.Body.UserId == assignCaseRequestDto.AssignedByUserId)
        {
            return;
        }

        var assigningUser = await _applicationUserService.GetAsync(new ApplicationUserId(assignCaseRequestDto.AssignedByUserId), stoppingToken);

        if (!assigningUser.Roles.Any(x => x is UserRole.Manager or UserRole.Admin))
        {
            context.AddFailure(new ValidationFailure
            {
                ErrorMessage = "You cannot assign a case to another user.",
                ErrorCode = HttpStatusCode.Forbidden.ToString()
            });
        }
    }
}
