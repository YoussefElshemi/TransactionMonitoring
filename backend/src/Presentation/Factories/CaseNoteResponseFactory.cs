using Core.Models.CaseNotes;
using Core.ValueObjects;
using Presentation.Mappers.CaseNotes;
using Presentation.Models.CaseNotes;

namespace Presentation.Factories;

public class CaseNoteResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<CaseNoteResourceDto> CreateAsync(CaseNote caseNote, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([caseNote], context, stoppingToken);

        return resources[0];
    }

    public async Task<CaseNoteResourceDto[]> CreateAsync(
        IReadOnlyCollection<CaseNote> caseNotes, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (caseNotes.Count == 0)
        {
            return [];
        }

        var createdByUsers = await relatedRecordLoader.LoadUsersAsync(
            caseNotes.Select(x => (ApplicationUserId?)x.CreatedByUserId), context, stoppingToken);

        return caseNotes.Select(x => new CaseNoteResourceDto
        {
            CaseNote = x.ToDto(),
            CreatedByUser = createdByUsers.GetValueOrDefault(x.CreatedByUserId)
        }).ToArray();
    }
}
