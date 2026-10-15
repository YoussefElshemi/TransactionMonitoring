using Core.Interfaces.Repositories;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TransactionSchemaRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : ITransactionSchemaRepository
{
    public async Task AddAsync(TransactionSchema transactionSchema, CancellationToken stoppingToken)
    {
        var transactionSchemaEntity = transactionSchema.ToEntity();
        await context.TransactionSchemas.AddAsync(transactionSchemaEntity, stoppingToken);
    }

    public async Task ReplaceFieldsAsync(
        OrganizationId organizationId,
        List<SchemaField> fields,
        TransactionSchemaVersionId currentVersionId,
        VersionNumber currentVersionNumber,
        CancellationToken stoppingToken)
    {
        var transactionSchemaEntity = await context.TransactionSchemas
            .FirstAsync(x => x.OrganizationId == organizationId, stoppingToken);

        transactionSchemaEntity.Fields = fields.ToBase64String();
        transactionSchemaEntity.CurrentVersionId = currentVersionId;
        transactionSchemaEntity.CurrentVersionNumber = currentVersionNumber;
        transactionSchemaEntity.UpdatedAt = timeProvider.GetUtcNow();

        transactionSchemaEntity.IsDeleted = false;
        transactionSchemaEntity.DeletedAt = null;

        context.TransactionSchemas.Update(transactionSchemaEntity);
    }

    public async Task<TransactionSchema?> FindAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var transactionSchemaEntity = await Query(includeDeleted)
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId, stoppingToken);

        return transactionSchemaEntity?.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await Query(includeDeleted)
            .AnyAsync(x => x.OrganizationId == organizationId, stoppingToken);

        return new Exists(exists);
    }

    public async Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var transactionSchemaEntity = await context.TransactionSchemas
            .FirstAsync(x => x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken);

        transactionSchemaEntity.IsDeleted = true;
        transactionSchemaEntity.DeletedAt = timeProvider.GetUtcNow();
        transactionSchemaEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.TransactionSchemas.Update(transactionSchemaEntity);
    }

    public async Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var transactionSchemaEntity = await context.TransactionSchemas
            .FirstAsync(x => x.OrganizationId == organizationId && x.IsDeleted, stoppingToken);

        transactionSchemaEntity.IsDeleted = false;
        transactionSchemaEntity.DeletedAt = null;
        transactionSchemaEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.TransactionSchemas.Update(transactionSchemaEntity);
    }

    private IQueryable<TransactionSchemaEntity> Query(IncludeDeleted includeDeleted)
    {
        return includeDeleted
            ? context.TransactionSchemas
            : context.TransactionSchemas.Where(x => !x.IsDeleted);
    }
}
