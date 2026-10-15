using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.CaseLinks;

namespace Presentation.Validators.CaseLinks;

public class CaseLinkRequestValidator : AbstractValidator<CaseLinkRequestDto>
{
    public CaseLinkRequestValidator(ICaseService caseService, ICaseLinkService caseLinkService)
    {
        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found");

        RuleFor(x => x.CaseLinkId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseLinkService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new CaseLinkId(x.CaseLinkId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case Link not found");
    }
}
