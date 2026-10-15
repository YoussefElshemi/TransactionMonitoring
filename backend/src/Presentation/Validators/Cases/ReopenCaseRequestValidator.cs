using System.Net;
using Core.Extensions;
using Core.Interfaces.Services;
using Core.Models.Cases;
using Core.Models.Organizations;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Cases;

namespace Presentation.Validators.Cases;

public class ReopenCaseRequestValidator : AbstractValidator<ReopenCaseRequestDto>
{
    public ReopenCaseRequestValidator(ICaseService caseService, IOrganizationUserService organizationUserService)
    {
        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found")
            .MustAsync(async (x, _, ct) =>
            {
                var @case = await caseService.GetAsync(new CaseId(x.CaseId), ct);

                return @case.Status.IsTerminal();
            })
            .WithMessage($"{nameof(Case)} is still open and cannot be reopened");

        RuleFor(x => x.Body.UserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationUserService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApplicationUserId(x.Body.UserId), new IncludeDeleted(false), ct))
            .WithMessage($"User was not found in {nameof(Organization)}");

        RuleFor(x => x.Body.Reason)
            .NotEmpty()
            .WithMessage("Reason is required to reopen a case");
    }
}
