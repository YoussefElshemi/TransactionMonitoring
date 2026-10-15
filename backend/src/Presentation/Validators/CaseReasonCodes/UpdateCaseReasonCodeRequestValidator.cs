using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.CaseReasonCodes;

namespace Presentation.Validators.CaseReasonCodes;

public class UpdateCaseReasonCodeRequestValidator : AbstractValidator<UpdateCaseReasonCodeRequestDto>
{
    public UpdateCaseReasonCodeRequestValidator(IOrganizationService organizationService, ICaseReasonCodeService caseReasonCodeService)
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
            .WithMessage("Case reason code not found");

        RuleFor(x => x.Body.Description)
            .MaximumLength(CaseReasonCodeDescription.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Description));

        RuleFor(x => x.Body.Disposition)
            .IsEnumName(typeof(CaseDisposition), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Disposition));

        RuleFor(x => x.Body.ClearDisposition)
            .Must(x => x != true)
            .WithMessage("ClearDisposition cannot be sent alongside a Disposition")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Disposition));
    }
}
