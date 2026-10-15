using Core.Enums;
using Core.Interfaces;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Interceptors;

public class ModeStampingSaveChangesInterceptor(IModeContext modeContext) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken stoppingToken = default)
    {
        Stamp(eventData.Context);

        return base.SavingChangesAsync(eventData, result, stoppingToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var addedEntries = context.ChangeTracker
            .Entries<IModeScoped>()
            .Where(x => x.State is EntityState.Added);

        foreach (var addedEntry in addedEntries)
        {
            // Interface members can't be `required`, and materialisation bypasses initialisers, so an unset mode falls back to the caller's.
            if (addedEntry.Entity.ModeId == default)
            {
                addedEntry.Property(nameof(IModeScoped.ModeId)).CurrentValue = (int)modeContext.CurrentOrLive;

                continue;
            }

            if (modeContext.Current is not { } current || addedEntry.Entity.ModeId == (int)current)
            {
                continue;
            }

            throw new InvalidOperationException(
                $"{addedEntry.Metadata.DisplayName()} was written in {(Mode)addedEntry.Entity.ModeId} " +
                $"while the caller is in {current}.");
        }
    }
}
