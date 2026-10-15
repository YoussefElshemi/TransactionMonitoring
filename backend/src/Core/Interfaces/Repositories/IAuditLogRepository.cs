using Core.Models.AuditLogs;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IAuditLogRepository : IPagedRepository<ListAuditLogRequest, AuditLog>
{
    Task RecordRevealAsync(RecordRevealRequest recordRevealRequest, CancellationToken stoppingToken);

    Task<TotalCount> CountEntriesAsync(ExportAuditLogRequest exportAuditLogRequest, CancellationToken stoppingToken);

    Task<List<AuditLogEntry>> ListEntriesAsync(ExportAuditLogRequest exportAuditLogRequest, CancellationToken stoppingToken);

    Task<List<AuditLogEntry>> ListChainAsync(
        OrganizationId organizationId,
        Sequence? afterSequence,
        Count batchSize,
        DatedRequest datedRequest,
        CancellationToken stoppingToken);

    Task<AuditLogEntry?> FindEntryBeforeAsync(OrganizationId organizationId, Sequence sequence, CancellationToken stoppingToken);

    Task<IReadOnlyDictionary<Sequence, Hash>> ListHashesAtAsync(
        OrganizationId organizationId,
        IReadOnlyCollection<Sequence> sequences,
        CancellationToken stoppingToken);

    Task<List<AuditChainHead>> ListChainHeadsAsync(CancellationToken stoppingToken);
}
