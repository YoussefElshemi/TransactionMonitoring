using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.CaseReasonCodes;

namespace Presentation.Validators.CaseReasonCodes;

public class DeleteCaseReasonCodeRequestValidator : AbstractValidator<DeleteCaseReasonCodeRequestDto>
{
    public DeleteCaseReasonCodeRequestValidator(IOrganizationService organizationService, ICaseReasonCodeService caseReasonCodeService)
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
            .MustAsync(async (x, _, ct) => await caseReasonCodeService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseReasonCodeId(x.CaseReasonCodeId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case reason code not found")

            .MustAsync(async (x, _, ct) =>
            {
                var usage = await caseReasonCodeService.GetUsageAsync(new CaseReasonCodeId(x.CaseReasonCodeId), ct);

                return usage.CaseCount == 0;
            })
            .WithMessage("Case reason code has been used to close a case and cannot be deleted. Disable it instead.");
    }
}
