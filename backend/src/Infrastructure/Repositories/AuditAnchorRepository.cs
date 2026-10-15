using Core.Interfaces.Repositories;
using Core.Models.AuditLogs;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AuditAnchorRepository(TransactionMonitoringDbContext context) : IAuditAnchorRepository
{
    public async Task AddAsync(AuditAnchor auditAnchor, CancellationToken stoppingToken)
    {
        await context.AuditAnchors.AddAsync(auditAnchor.ToEntity(), stoppingToken);
    }

    public async Task<List<AuditAnchor>> ListAsync(
        OrganizationId organizationId,
        DatedRequest datedRequest,
        CancellationToken stoppingToken)
    {
        var queryable = context.AuditAnchors.Where(x => x.OrganizationId == (Guid)organizationId);

        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        var auditAnchorEntities = await queryable
            .OrderBy(x => x.Sequence)
            .ToListAsync(stoppingToken);

        return auditAnchorEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<List<AuditAnchor>> ListLatestAsync(CancellationToken stoppingToken)
    {
        var auditAnchorEntities = await context.AuditAnchors
            .FromSqlRaw(
                $"""
                 SELECT DISTINCT ON (a."OrganizationId") a.*
                 FROM "{Schemas.Service}"."{TableNames.AuditAnchors}" a
                 ORDER BY a."OrganizationId", a."Sequence" DESC
                 """)
            .ToListAsync(stoppingToken);

        return auditAnchorEntities.ConvertAll(x => x.ToModel());
    }
}
