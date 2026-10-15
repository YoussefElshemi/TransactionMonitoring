using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.CaseNotes;
using Presentation.Models.CaseNotes;
using Presentation.Validators.Common;

namespace Presentation.Validators.CaseNotes;

public class ListCaseNoteRequestValidator(
    ICaseNoteService caseNoteService,
    TimeProvider timeProvider) : PagedRequestValidator<ListCaseNoteRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListCaseNoteRequestDto request, CancellationToken stoppingToken)
    {
        return caseNoteService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}