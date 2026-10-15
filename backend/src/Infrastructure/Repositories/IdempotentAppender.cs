using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Repositories;

// The read is the fast path; the unique index is the arbiter. On a violation the loser detaches, reads back the winner and inserts only what's missing. Saves directly, so only for ingest paths that own their writes.
internal static class IdempotentAppender
{
    public static async Task AppendAsync<TEntity, TKey>(
        DbContext context,
        IReadOnlyList<TEntity> entities,
        Func<CancellationToken, Task<List<TKey>>> readRecordedAsync,
        Func<TEntity, TKey> keyOf,
        CancellationToken stoppingToken)
        where TEntity : class
        where TKey : notnull
    {
        if (entities.Count == 0)
        {
            return;
        }

        var recorded = await readRecordedAsync(stoppingToken);
        var pending = Missing(entities, recorded, keyOf);

        if (pending.Count == 0)
        {
            return;
        }

        try
        {
            await AddAndSaveAsync(context, pending, stoppingToken);
        }
        catch (DbUpdateException)
        {
            recorded = await readRecordedAsync(stoppingToken);
            pending = Missing(pending, recorded, keyOf);

            if (pending.Count == 0)
            {
                return;
            }

            // A second violation isn't a race: something other than concurrency is wrong.
            await AddAndSaveAsync(context, pending, stoppingToken);
        }
    }

    // Detach before throwing, or the caller's failure-status write would retry these rows and throw again.
    private static async Task AddAndSaveAsync<TEntity>(
        DbContext context,
        List<TEntity> entities,
        CancellationToken stoppingToken)
        where TEntity : class
    {
        var entries = await AddAsync(context, entities, stoppingToken);

        try
        {
            await context.SaveChangesAsync(stoppingToken);
        }
        catch
        {
            foreach (var entry in entries)
            {
                entry.State = EntityState.Detached;
            }

            throw;
        }
    }

    private static async Task<List<EntityEntry<TEntity>>> AddAsync<TEntity>(
        DbContext context,
        List<TEntity> entities,
        CancellationToken stoppingToken)
        where TEntity : class
    {
        var entries = new List<EntityEntry<TEntity>>(entities.Count);

        foreach (var entity in entities)
        {
            entries.Add(await context.AddAsync(entity, stoppingToken));
        }

        return entries;
    }

    private static List<TEntity> Missing<TEntity, TKey>(
        IReadOnlyList<TEntity> entities,
        List<TKey> recorded,
        Func<TEntity, TKey> keyOf)
        where TKey : notnull
    {
        var keys = recorded.ToHashSet();

        return entities.Where(x => keys.Add(keyOf(x))).ToList();
    }
}
