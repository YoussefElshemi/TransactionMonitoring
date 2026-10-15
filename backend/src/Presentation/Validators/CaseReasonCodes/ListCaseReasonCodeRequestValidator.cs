using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.CaseReasonCodes;

namespace Presentation.Validators.CaseReasonCodes;

public class ListCaseReasonCodeRequestValidator : AbstractValidator<ListCaseReasonCodeRequestDto>
{
    public ListCaseReasonCodeRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Disposition)
            .IsEnumName(typeof(CaseDisposition), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Disposition));
    }
}
