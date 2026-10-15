using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.CaseLinks;

namespace Presentation.Validators.CaseLinks;

public class CreateCaseLinkRequestValidator : AbstractValidator<CreateCaseLinkRequestDto>
{
    public CreateCaseLinkRequestValidator(ICaseService caseService, IOrganizationService organizationService)
    {
        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found");

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.LinkedCaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, linkedCaseId, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(linkedCaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Linked case not found");

        RuleFor(x => x.Body.Reason)
            .MaximumLength(2048)
            .When(x => x.Body.Reason != null);
    }
}
