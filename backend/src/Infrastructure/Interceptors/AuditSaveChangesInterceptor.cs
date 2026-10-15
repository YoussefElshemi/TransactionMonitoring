using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Infrastructure.Entities;
using Infrastructure.Entities.BaseEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Interceptors;

public class AuditSaveChangesInterceptor(
    IAuditContext auditContext,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken stoppingToken = default)
    {
        AddAuditLogs(eventData.Context);

        return base.SavingChangesAsync(eventData, result, stoppingToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditLogs(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private void AddAuditLogs(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var changedRelationOwners = ChangedRelationOwners(context);

        var auditableEntries = context.ChangeTracker
            .Entries<IAuditable>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted ||
                        changedRelationOwners.Contains(KeyOf(x.Entity)))
            .ToList();

        foreach (var auditableEntry in auditableEntries)
        {
            var auditLog = BuildAuditLog(auditableEntry, changedRelationOwners.Contains(KeyOf(auditableEntry.Entity)));
            if (auditLog is not null)
            {
                context.Add(auditLog);
            }
        }
    }

    private static HashSet<AuditedEntityKey> ChangedRelationOwners(DbContext context)
    {
        var owners = new HashSet<AuditedEntityKey>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                AuditedRelationRegistry.TryGetOwner(entry.Entity, out var owner))
            {
                owners.Add(owner);
            }
        }

        return owners;
    }

    private static AuditedEntityKey KeyOf(IAuditable auditable)
    {
        return new AuditedEntityKey(auditable.AuditEntityType, auditable.AuditEntityId);
    }

    private AuditLogEntity? BuildAuditLog(EntityEntry<IAuditable> entry, bool hasRelationChanges)
    {
        var operation = ResolveOperation(entry);

        if (operation == AuditOperation.Updated && !hasRelationChanges && !HasAuditedChanges(entry))
        {
            return null;
        }

        var auditable = entry.Entity;

        return new AuditLogEntity
        {
            AuditLogId = Guid.NewGuid(),
            EntityTypeId = (int)auditable.AuditEntityType,
            EntityId = auditable.AuditEntityId,
            OrganizationId = auditable.AuditOrganizationId,
            OperationId = (int)operation,
            Snapshot = BuildSnapshot(entry),
            ChangedByUserId = auditContext.ActingUserId,
            ChangedByApiClientId = auditContext.ActingApiClientId,
            CreatedAt = timeProvider.GetUtcNow()
        };
    }

    private static bool HasAuditedChanges(EntityEntry<IAuditable> entry)
    {
        var auditedNames = entry.Entity.AuditedProperties
            .Select(property => property.Name)
            .ToHashSet();

        return entry.Properties
            .Any(property => property.IsModified && auditedNames.Contains(property.Metadata.Name));
    }

    private static AuditOperation ResolveOperation(EntityEntry<IAuditable> entry)
    {
        if (entry.State == EntityState.Added)
        {
            return AuditOperation.Created;
        }

        if (entry.State == EntityState.Deleted)
        {
            return AuditOperation.Deleted;
        }

        var isDeletedProperty = entry.Properties
            .FirstOrDefault(x => x.Metadata.Name == nameof(DeletableEntity.IsDeleted));

        if (isDeletedProperty is { IsModified: true, CurrentValue: true })
        {
            return AuditOperation.Deleted;
        }

        return AuditOperation.Updated;
    }

    private static string BuildSnapshot(EntityEntry<IAuditable> entry)
    {
        var snapshot = new Dictionary<string, object?>();

        foreach (var property in entry.Entity.AuditedProperties)
        {
            if (property.SnapshotSelector is null)
            {
                snapshot[property.Name] = entry.CurrentValues[property.Name];
                continue;
            }

            var value = property.SnapshotSelector(entry);
            if (value is not null)
            {
                snapshot[property.Name] = value;
            }
        }

        return snapshot.Serialize();
    }
}
