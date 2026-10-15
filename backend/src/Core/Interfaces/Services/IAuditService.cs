using Core.Models.AuditLogs;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IAuditService : IPagedService<ListAuditLogRequest, AuditLog>
{
    Task RecordRevealAsync(RecordRevealRequest recordRevealRequest, CancellationToken stoppingToken);

    Task<AuditLogExport> ExportAsync(ExportAuditLogRequest exportAuditLogRequest, CancellationToken stoppingToken);

    Task<AuditChainVerification> VerifyAsync(VerifyAuditChainRequest verifyAuditChainRequest, CancellationToken stoppingToken);

    Task<Count> AnchorAsync(CancellationToken stoppingToken);
}
