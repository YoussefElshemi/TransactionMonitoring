using Core.Models.AuditLogs;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IAuditAnchorRepository
{
    Task AddAsync(AuditAnchor auditAnchor, CancellationToken stoppingToken);

    Task<List<AuditAnchor>> ListAsync(OrganizationId organizationId, DatedRequest datedRequest, CancellationToken stoppingToken);

    Task<List<AuditAnchor>> ListLatestAsync(CancellationToken stoppingToken);
}
