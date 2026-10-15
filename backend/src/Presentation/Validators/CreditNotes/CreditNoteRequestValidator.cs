using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.CreditNotes;

namespace Presentation.Validators.CreditNotes;

public class CreditNoteRequestValidator : AbstractValidator<CreditNoteRequestDto>
{
    public CreditNoteRequestValidator(
        IOrganizationService organizationService,
        ICreditNoteService creditNoteService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.CreditNoteId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (dto, x, _, ct) =>
                await creditNoteService.ExistsAsync(new OrganizationId(dto.OrganizationId), new CreditNoteId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Credit note not found");
    }
}
