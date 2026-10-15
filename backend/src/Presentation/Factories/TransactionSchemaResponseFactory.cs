using Core.Models.TransactionSchemas;
using Core.ValueObjects;
using Presentation.Mappers.TransactionSchemas;
using Presentation.Models.TransactionSchemas;

namespace Presentation.Factories;

public class TransactionSchemaResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<TransactionSchemaResourceDto> CreateAsync(
        TransactionSchema transactionSchema, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            [transactionSchema.OrganizationId], context, stoppingToken);

        return new TransactionSchemaResourceDto
        {
            TransactionSchema = transactionSchema.ToDto(),
            Organization = organizations.GetValueOrDefault(transactionSchema.OrganizationId)
        };
    }

    public async Task<TransactionSchemaVersionResourceDto> CreateAsync(
        TransactionSchemaVersion transactionSchemaVersion, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([transactionSchemaVersion], context, stoppingToken);

        return resources[0];
    }

    public async Task<TransactionSchemaVersionResourceDto[]> CreateAsync(
        IReadOnlyCollection<TransactionSchemaVersion> transactionSchemaVersions, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (transactionSchemaVersions.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            transactionSchemaVersions.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);

        return transactionSchemaVersions.Select(x => new TransactionSchemaVersionResourceDto
        {
            TransactionSchemaVersion = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId)
        }).ToArray();
    }
}
