using System.Net;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces.Services;
using Core.Models.Cases;
using Core.Models.Organizations;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Cases;

namespace Presentation.Validators.Cases;

public class ChangeCaseStatusRequestValidator : AbstractValidator<ChangeCaseStatusRequestDto>
{
    public ChangeCaseStatusRequestValidator(ICaseService caseService, IOrganizationUserService organizationUserService)
    {
        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found");

        RuleFor(x => x.Body.UserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationUserService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApplicationUserId(x.Body.UserId), new IncludeDeleted(false), ct))
            .WithMessage($"User was not found in {nameof(Organization)}")
            .MustAsync(async (x, _, ct) =>
            {
                var @case = await caseService.GetAsync(new CaseId(x.CaseId), ct);

                return @case.AssignedToUserId == x.Body.UserId;
            })
            .WithMessage($"{nameof(Case)} must be assigned to user attempting to change its status");

        RuleFor(x => x.Body.Status)
            .Cascade(CascadeMode.Stop)
            .IsEnumName(typeof(CaseStatus), false)
            .MustAsync(async (x, status, ct) =>
            {
                var @case = await caseService.GetAsync(new CaseId(x.CaseId), ct);

                return @case.Status.CanTransitionTo(Enum.Parse<CaseStatus>(status, true));
            })
            .WithMessage(x => $"{nameof(Case)} cannot move to '{x.Body.Status}' from its current status");
    }
}
