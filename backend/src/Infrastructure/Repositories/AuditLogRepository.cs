using System.Text.Json.Nodes;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.AuditLogs;
using Core.Models.Common;
using Core.Services;
using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AuditLogRepository(
    TransactionMonitoringDbContext context,
    IAuditContext auditContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : PagedRepository<ListAuditLogRequest, AuditLogEntity, AuditLog>, IAuditLogRepository
{
    public async Task RecordRevealAsync(RecordRevealRequest recordRevealRequest, CancellationToken stoppingToken)
    {
        await context.AuditLogs.AddAsync(new AuditLogEntity
        {
            AuditLogId = Guid.NewGuid(),
            EntityTypeId = (int)recordRevealRequest.EntityType,
            EntityId = recordRevealRequest.EntityId,
            OrganizationId = recordRevealRequest.OrganizationId,
            OperationId = (int)AuditOperation.Revealed,

            Snapshot = new Dictionary<string, object?>
            {
                [nameof(RecordRevealRequest.Reason)] = recordRevealRequest.Reason.ToString()
            }.Serialize(),
            ChangedByUserId = auditContext.ActingUserId,
            ChangedByApiClientId = auditContext.ActingApiClientId,
            CreatedAt = timeProvider.GetUtcNow()
        }, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task<TotalCount> CountEntriesAsync(ExportAuditLogRequest exportAuditLogRequest, CancellationToken stoppingToken)
    {
        var count = await FilterForExport(exportAuditLogRequest).CountAsync(stoppingToken);

        return new TotalCount(count);
    }

    public async Task<List<AuditLogEntry>> ListEntriesAsync(
        ExportAuditLogRequest exportAuditLogRequest,
        CancellationToken stoppingToken)
    {
        var auditLogEntities = await FilterForExport(exportAuditLogRequest)
            .OrderBy(x => x.Sequence)
            .ToListAsync(stoppingToken);

        return auditLogEntities.ConvertAll(x => x.ToEntry());
    }

    public async Task<List<AuditLogEntry>> ListChainAsync(
        OrganizationId organizationId,
        Sequence? afterSequence,
        Count batchSize,
        DatedRequest datedRequest,
        CancellationToken stoppingToken)
    {
        var after = afterSequence.HasValue ? (long)afterSequence.Value : 0;

        var auditLogEntities = await FilterByDateRange(
                context.AuditLogs.Where(x => x.OrganizationId == (Guid)organizationId && x.Sequence > after), datedRequest)
            .OrderBy(x => x.Sequence)
            .Take(batchSize)
            .ToListAsync(stoppingToken);

        return auditLogEntities.ConvertAll(x => x.ToEntry());
    }

    public async Task<AuditLogEntry?> FindEntryBeforeAsync(
        OrganizationId organizationId,
        Sequence sequence,
        CancellationToken stoppingToken)
    {
        var auditLogEntity = await context.AuditLogs
            .Where(x => x.OrganizationId == (Guid)organizationId && x.Sequence < sequence)
            .OrderByDescending(x => x.Sequence)
            .FirstOrDefaultAsync(stoppingToken);

        return auditLogEntity?.ToEntry();
    }

    public async Task<IReadOnlyDictionary<Sequence, Hash>> ListHashesAtAsync(
        OrganizationId organizationId,
        IReadOnlyCollection<Sequence> sequences,
        CancellationToken stoppingToken)
    {
        var positions = sequences.Select(x => (long)x).Distinct().ToList();

        var hashes = await context.AuditLogs
            .Where(x => x.OrganizationId == (Guid)organizationId && positions.Contains(x.Sequence))
            .Select(x => new { x.Sequence, x.Hash })
            .ToListAsync(stoppingToken);

        return hashes.ToDictionary(x => new Sequence(x.Sequence), x => new Hash(x.Hash));
    }

    public async Task<List<AuditChainHead>> ListChainHeadsAsync(CancellationToken stoppingToken)
    {
        var auditLogEntities = await context.AuditLogs
            .FromSqlRaw(
                $"""
                 SELECT DISTINCT ON (a."OrganizationId") a.*
                 FROM "{Schemas.Service}"."{TableNames.AuditLogs}" a
                 ORDER BY a."OrganizationId", a."Sequence" DESC
                 """)
            .ToListAsync(stoppingToken);

        return auditLogEntities.ConvertAll(x => new AuditChainHead
        {
            OrganizationId = x.OrganizationId.HasValue ? new OrganizationId(x.OrganizationId.Value) : null,
            Sequence = new Sequence(x.Sequence),
            Hash = new Hash(x.Hash)
        });
    }

    protected override IOrderByExpression<AuditLogEntity> OrderByExpression()
    {
        return new OrderByExpression<AuditLogEntity, DateTimeOffset>(x => x.CreatedAt);
    }

    protected override IQueryable<AuditLogEntity> FilterByPagedRequest(ListAuditLogRequest listAuditLogRequest)
    {
        return context.AuditLogs
            .Where(x => x.EntityTypeId == (int)listAuditLogRequest.EntityType &&
                        x.EntityId == listAuditLogRequest.EntityId &&
                        x.OrganizationId == (Guid)listAuditLogRequest.OrganizationId);
    }

    protected override IQueryable<AuditLogEntity> FilterByDateRange(IQueryable<AuditLogEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }

    protected override async Task<List<AuditLog>> MapPageAsync(
        List<AuditLogEntity> items,
        ListAuditLogRequest request,
        CancellationToken stoppingToken)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var oldest = items[^1];
        var previousSnapshot = await context.AuditLogs
            .Where(x => x.EntityTypeId == (int)request.EntityType &&
                        x.EntityId == request.EntityId &&
                        x.OrganizationId == (Guid)request.OrganizationId &&
                        x.CreatedAt < oldest.CreatedAt &&
                        x.OperationId != (int)AuditOperation.Revealed)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Snapshot)
            .FirstOrDefaultAsync(stoppingToken);

        var auditedProperties = AuditedPropertyRegistry.For(request.EntityType);
        var models = new List<AuditLog>(items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].OperationId == (int)AuditOperation.Revealed)
            {
                var reason = items[i].Snapshot.TryDeserialize<JsonObject>();

                models.Add(items[i].ToModel(AuditLogChangeCalculator.Compute(null, reason)));

                continue;
            }

            var current = items[i].Snapshot.TryDeserialize<JsonObject>();
            var previous = PreviousSnapshotOf(items, i, previousSnapshot)?.TryDeserialize<JsonObject>();

            var rawChanges = AuditLogChangeCalculator.Compute(previous, current);
            var changes = AuditedChangeMapper.Apply(rawChanges, auditedProperties);

            models.Add(items[i].ToModel(changes));
        }

        return models;
    }

    private IQueryable<AuditLogEntity> FilterForExport(ExportAuditLogRequest exportAuditLogRequest)
    {
        var queryable = context.AuditLogs
            .Where(x => x.OrganizationId == (Guid)exportAuditLogRequest.OrganizationId);

        if (exportAuditLogRequest is { EntityType: not null, EntityId: not null })
        {
            var entityTypeId = (int)exportAuditLogRequest.EntityType.Value;
            var entityId = exportAuditLogRequest.EntityId.Value;

            queryable = queryable.Where(x => x.EntityTypeId == entityTypeId && x.EntityId == entityId);
        }

        return FilterByDateRange(queryable, exportAuditLogRequest);
    }

    private static string? PreviousSnapshotOf(List<AuditLogEntity> items, int index, string? fallback)
    {
        for (var i = index + 1; i < items.Count; i++)
        {
            if (items[i].OperationId != (int)AuditOperation.Revealed)
            {
                return items[i].Snapshot;
            }
        }

        return fallback;
    }
}
