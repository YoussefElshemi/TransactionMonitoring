using Core.Models.CaseReasonCodes;
using Core.ValueObjects;
using Presentation.Mappers.CaseReasonCodes;
using Presentation.Models.CaseReasonCodes;

namespace Presentation.Factories;

public class CaseReasonCodeResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<CaseReasonCodeResourceDto> CreateAsync(
        CaseReasonCode caseReasonCode, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([caseReasonCode], context, stoppingToken);

        return resources[0];
    }

    public async Task<CaseReasonCodeResourceDto[]> CreateAsync(
        IReadOnlyCollection<CaseReasonCode> caseReasonCodes, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (caseReasonCodes.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            caseReasonCodes.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);

        return caseReasonCodes.Select(x => new CaseReasonCodeResourceDto
        {
            CaseReasonCode = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId)
        }).ToArray();
    }
}
