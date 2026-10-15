using Core.Models.TransactionSchemas;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ITransactionSchemaVersionRepository : IPagedRepository<ListTransactionSchemaVersionRequest, TransactionSchemaVersion>
{
    Task AddAsync(TransactionSchemaVersion transactionSchemaVersion, CancellationToken stoppingToken);
    Task<TransactionSchemaVersion> GetByVersionNumberAsync(TransactionSchemaId transactionSchemaId, VersionNumber versionNumber, CancellationToken stoppingToken);
    Task<VersionNumber> GetMaxVersionNumberAsync(TransactionSchemaId transactionSchemaId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, VersionNumber versionNumber, CancellationToken stoppingToken);
}
