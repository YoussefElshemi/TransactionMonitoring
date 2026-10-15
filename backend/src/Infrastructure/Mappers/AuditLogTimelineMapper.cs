using System.Text.Json.Nodes;
using Core.Enums;
using Core.Extensions;
using Core.Models.AuditLogs;
using Core.Services;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class AuditLogTimelineMapper
{
    public static List<AuditLog> ToTimeline(this List<AuditLogEntity> auditLogEntities)
    {
        var baselines = new Dictionary<(int EntityTypeId, Guid EntityId), string>();
        var timeline = new List<AuditLog>(auditLogEntities.Count);

        foreach (var auditLogEntity in auditLogEntities.OrderBy(x => x.CreatedAt))
        {
            if (auditLogEntity.OperationId == (int)AuditOperation.Revealed)
            {
                var reason = auditLogEntity.Snapshot.TryDeserialize<JsonObject>();

                timeline.Add(auditLogEntity.ToModel(AuditLogChangeCalculator.Compute(null, reason)));

                continue;
            }

            var key = (auditLogEntity.EntityTypeId, auditLogEntity.EntityId);
            var current = auditLogEntity.Snapshot.TryDeserialize<JsonObject>();
            var previous = baselines.TryGetValue(key, out var snapshot) ? snapshot.TryDeserialize<JsonObject>() : null;

            var rawChanges = AuditLogChangeCalculator.Compute(previous, current);
            var auditedProperties = AuditedPropertyRegistry.For((AuditableEntityType)auditLogEntity.EntityTypeId);

            timeline.Add(auditLogEntity.ToModel(AuditedChangeMapper.Apply(rawChanges, auditedProperties)));

            baselines[key] = auditLogEntity.Snapshot;
        }

        return timeline;
    }
}
