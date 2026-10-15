using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Sandbox;
using Core.ValueObjects;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace Infrastructure.Repositories;

public class SandboxRepository(TransactionMonitoringDbContext context) : ISandboxRepository
{
    // Kept on reset: Usage is the credit ledger, API clients are the caller's credentials, and idempotency records include this request's own. Retaining is only safe while nothing they reference is deleted.
    private static readonly HashSet<Type> Retained =
    [
        typeof(UsageEntity),
        typeof(ApiClientEntity),
        typeof(ApiClientSecretEntity),
        typeof(IdempotencyRecordEntity)
    ];

    public async Task<List<SandboxResetCount>> ResetAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var counts = new List<SandboxResetCount>();

        foreach (var entityType in DeletionOrder())
        {
            var schema = entityType.GetSchema() ?? "public";
            var table = entityType.GetTableName()!;
            var storeObject = StoreObjectIdentifier.Table(table, entityType.GetSchema());

            if (Retained.Contains(entityType.ClrType))
            {
                continue;
            }

            var organizationColumn = ColumnName(entityType, storeObject, nameof(ApiClientEntity.OrganizationId));
            var modeColumn = ColumnName(entityType, storeObject, nameof(IModeScoped.ModeId));

            if (organizationColumn is null || modeColumn is null)
            {
                continue;
            }

            var sql = $"""
                       DELETE FROM "{schema}"."{table}"
                       WHERE "{modeColumn}" = {(int)Mode.Test} AND "{organizationColumn}" = @organizationId
                       """;

            var deleted = await context.Database.ExecuteSqlRawAsync(
                sql,
                [new NpgsqlParameter("organizationId", (Guid)organizationId)],
                stoppingToken);

            if (deleted > 0)
            {
                counts.Add(new SandboxResetCount
                {
                    Resource = new Resource(table),
                    Count = new Count(deleted)
                });
            }
        }

        return counts;
    }

    // Dependents first, so nothing is deleted while a row still references it.
    private List<IEntityType> DeletionOrder()
    {
        var modeScoped = context.Model.GetEntityTypes()
            .Where(x => typeof(IModeScoped).IsAssignableFrom(x.ClrType) && x.BaseType is null)
            .ToList();

        var byClrType = modeScoped.ToDictionary(x => x.ClrType);
        var ordered = new List<IEntityType>();
        var visited = new HashSet<Type>();

        foreach (var entityType in modeScoped)
        {
            Visit(entityType);
        }

        return ordered;

        void Visit(IEntityType entityType)
        {
            // Self-references are removed by one statement; marking visited on entry stops the walk looping.
            if (!visited.Add(entityType.ClrType))
            {
                return;
            }

            foreach (var dependent in modeScoped.Where(x => x.GetForeignKeys()
                         .Any(foreignKey => foreignKey.PrincipalEntityType.ClrType == entityType.ClrType &&
                                            foreignKey.DeclaringEntityType.ClrType != entityType.ClrType)))
            {
                if (byClrType.ContainsKey(dependent.ClrType))
                {
                    Visit(dependent);
                }
            }

            ordered.Add(entityType);
        }
    }

    private static string? ColumnName(IEntityType entityType, StoreObjectIdentifier storeObject, string propertyName)
    {
        return entityType.GetProperties().FirstOrDefault(x => x.Name == propertyName)?.GetColumnName(storeObject);
    }
}
