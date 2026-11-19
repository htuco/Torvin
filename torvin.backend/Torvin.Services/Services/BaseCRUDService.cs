using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Torvin.Model.Exceptions;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;

namespace Torvin.Services.Services;

// Kreiranje, izmjena i brisanje. Konkretan servis dodaje samo svoja pravila u Before* metode
public abstract class BaseCRUDService<TModel, TSearch, TEntity, TInsert, TUpdate>(
    TorvinDbContext context,
    ReferenceDataCache cache,
    ILogger logger)
    : BaseReadService<TModel, TSearch, TEntity>(context, cache), ICRUDService<TModel, TSearch, TInsert, TUpdate>
    where TModel : class
    where TSearch : BaseSearchObject
    where TEntity : BaseEntity
{
    private const string DuplicateMessage = "Zapis s istim nazivom ili kodom već postoji.";
    private const string InUseMessage = "Zapis se koristi i ne može se obrisati.";

    public async Task<TModel> InsertAsync(TInsert request, CancellationToken cancellationToken = default)
    {
        await BeforeInsertAsync(request, cancellationToken);
        // Mapster vraća null samo za null izvor, a request iz controllera nikad nije null
        var entity = request.Adapt<TEntity>()!;
        Context.Add(entity);
        await SaveAsync(entity, DuplicateMessage, cancellationToken);
        return entity.Adapt<TModel>();
    }

    public async Task<TModel> UpdateAsync(int id, TUpdate request, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        await BeforeUpdateAsync(id, request, cancellationToken);
        request.Adapt(entity);
        await SaveAsync(entity, DuplicateMessage, cancellationToken);
        return entity.Adapt<TModel>();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        await BeforeDeleteAsync(entity, cancellationToken);
        Context.Remove(entity);
        await SaveAsync(entity, InUseMessage, cancellationToken);
    }

    protected virtual Task BeforeInsertAsync(TInsert request, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task BeforeUpdateAsync(int id, TUpdate request, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    protected virtual Task BeforeDeleteAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;

    protected static void ThrowIfInUse(string subject, params (string Label, int Count)[] usages)
    {
        var used = usages.Where(usage => usage.Count > 0).Select(usage => $"{usage.Label}: {usage.Count}").ToList();
        if (used.Count > 0)
        {
            throw new BusinessException($"{subject} se koristi i ne može se obrisati ({string.Join(", ", used)}).");
        }
    }

    private async Task<TEntity> FindAsync(int id, CancellationToken cancellationToken) =>
        await Context.Set<TEntity>().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
        ?? throw new NotFoundException(NotFoundMessage);

    // Provjera prije snimanja ne pokriva dva istovremena zahtjeva, pa unique indeks i FK u bazi
    // odbiju drugi. Korisnik tada dobija istu poruku kao i kod provjere, a ne grešku servera
    private async Task SaveAsync(TEntity entity, string conflictMessage, CancellationToken cancellationToken)
    {
        try
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Baza je odbila izmjenu {Entity} {EntityId}", EntityName, entity.Id);
            throw new BusinessException(conflictMessage);
        }

        Cache.Invalidate(EntityName);
    }
}
