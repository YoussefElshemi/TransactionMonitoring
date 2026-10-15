using Core.Models.CaseLinks;
using Core.ValueObjects;
using Presentation.Mappers.CaseLinks;
using Presentation.Mappers.Cases;
using Presentation.Models.CaseLinks;

namespace Presentation.Factories;

public class CaseLinkResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<CaseLinkResourceDto> CreateAsync(CaseLink caseLink, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([caseLink], context, stoppingToken);

        return resources[0];
    }

    public async Task<CaseLinkResourceDto[]> CreateAsync(
        IReadOnlyCollection<CaseLink> caseLinks, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (caseLinks.Count == 0)
        {
            return [];
        }

        var createdByUsers = await relatedRecordLoader.LoadUsersAsync(
            caseLinks.Select(x => (ApplicationUserId?)x.CreatedByUserId), context, stoppingToken);

        return caseLinks.Select(x => new CaseLinkResourceDto
        {
            CaseLink = x.ToDto(),
            LinkedCase = x.LinkedCase?.ToDto(),
            CreatedByUser = createdByUsers.GetValueOrDefault(x.CreatedByUserId)
        }).ToArray();
    }
}
