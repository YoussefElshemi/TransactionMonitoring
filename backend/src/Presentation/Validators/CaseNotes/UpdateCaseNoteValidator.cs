using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.CaseNotes;

namespace Presentation.Validators.CaseNotes;

public class UpdateCaseNoteValidator : AbstractValidator<UpdateCaseNoteRequestDto>
{
    public UpdateCaseNoteValidator(ICaseService caseService, ICaseNoteService caseNoteService)
    {
        RuleFor(x => x.CaseNoteId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseNoteService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new CaseNoteId(x.CaseNoteId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case Note not found");

        RuleFor(x => x.CaseId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await caseService.ExistsAsync(new OrganizationId(x.OrganizationId), new CaseId(x.CaseId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Case not found");

        RuleFor(x => x.Body.Message)
            .MaximumLength(2048)
            .When(x => x.Body.Message != null);
    }
}