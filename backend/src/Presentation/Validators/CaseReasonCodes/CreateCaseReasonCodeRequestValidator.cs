using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.CaseReasonCodes;

namespace Presentation.Validators.CaseReasonCodes;

public class CreateCaseReasonCodeRequestValidator : AbstractValidator<CreateCaseReasonCodeRequestDto>
{
    public CreateCaseReasonCodeRequestValidator(IOrganizationService organizationService, ICaseReasonCodeService caseReasonCodeService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(CaseReasonCodeName.MaxLength)
            .MustAsync(async (x, _, ct) =>
            {
                var existing = await caseReasonCodeService.FindByCodeAsync(new OrganizationId(x.OrganizationId), new CaseReasonCodeName(x.Body.Code), ct);

                return existing is null;
            })
            .WithMessage("Code is already declared for this organization");

        RuleFor(x => x.Body.Description)
            .NotEmpty()
            .MaximumLength(CaseReasonCodeDescription.MaxLength);

        RuleFor(x => x.Body.Disposition)
            .IsEnumName(typeof(CaseDisposition), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Disposition));

        RuleFor(x => x.Body.IsEnabled)
            .NotNull();
    }
}
