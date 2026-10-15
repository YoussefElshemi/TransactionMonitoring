using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.CaseReasonCodes;

namespace Presentation.Validators.CaseReasonCodes;

public class RecoverCaseReasonCodeRequestValidator : AbstractValidator<RecoverCaseReasonCodeRequestDto>
{
    public RecoverCaseReasonCodeRequestValidator(IOrganizationService organizationService, ICaseReasonCodeService caseReasonCodeService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.CaseReasonCodeId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseReasonCodeService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseReasonCodeId(x.CaseReasonCodeId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case reason code not found")
            .MustAsync(async (x, _, ct) =>
            {
                var retired = await caseReasonCodeService.GetAsync(new CaseReasonCodeId(x.CaseReasonCodeId), new IncludeDeleted(true), ct);
                var holder = await caseReasonCodeService.FindByCodeAsync(new OrganizationId(x.OrganizationId), retired.Code, ct);

                return holder is null;
            })
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithMessage("Case reason code is already declared for this organization");
    }
}
