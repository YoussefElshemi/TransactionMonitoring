using Core.Models.TransactionSchemas;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ITransactionSchemaRepository
{
    Task AddAsync(TransactionSchema transactionSchema, CancellationToken stoppingToken);
    Task ReplaceFieldsAsync(OrganizationId organizationId, List<SchemaField> fields, TransactionSchemaVersionId currentVersionId, VersionNumber currentVersionNumber, CancellationToken stoppingToken);
    Task<TransactionSchema?> FindAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken);
}
