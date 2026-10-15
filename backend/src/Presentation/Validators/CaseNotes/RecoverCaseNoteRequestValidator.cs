using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.CaseNotes;

namespace Presentation.Validators.CaseNotes;

public class RecoverCaseNoteRequestValidator : AbstractValidator<RecoverCaseNoteRequestDto>
{
    public RecoverCaseNoteRequestValidator(ICaseService caseService, ICaseNoteService caseNoteService)
    {
        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found");

        RuleFor(x => x.CaseNoteId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseNoteService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new CaseNoteId(x.CaseNoteId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case Note not found");
    }
}