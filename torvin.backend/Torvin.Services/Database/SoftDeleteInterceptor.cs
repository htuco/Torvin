using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Torvin.Services.Database;

// Remove() nad zapisom koji se briše "mekano" postaje izmjena: red ostaje, a filter ga skriva
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        MarkDeleted(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        MarkDeleted(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void MarkDeleted(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                // Mijenjaju se samo ove dvije kolone, ostale ostaju kakve su u bazi
                entry.State = EntityState.Unchanged;
                entry.Property(e => e.IsDeleted).CurrentValue = true;
                entry.Property(e => e.IsDeleted).IsModified = true;
                entry.Property(e => e.DeletedAt).CurrentValue = DateTime.UtcNow;
                entry.Property(e => e.DeletedAt).IsModified = true;
            }
        }
    }
}
