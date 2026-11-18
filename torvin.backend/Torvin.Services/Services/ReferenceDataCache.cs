using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Torvin.Services.Services;

// Referentni podaci se čitaju za svaki dropdown, a mijenjaju rijetko. Izmjena poveća verziju entiteta,
// pa se stari unosi više ne čitaju: i čitanje koje je počelo prije izmjene upiše pod staru verziju
public sealed class ReferenceDataCache(IMemoryCache cache)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, int> _versions = new();

    public async Task<T> GetOrCreateAsync<T>(string entity, int pageSize, Func<Task<T>> load)
    {
        var version = _versions.GetValueOrDefault(entity);
        var value = await cache.GetOrCreateAsync($"reference:{entity}:v{version}:{pageSize}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Lifetime;
            return load();
        });
        return value!;
    }

    public void Invalidate(string entity) => _versions.AddOrUpdate(entity, 1, (_, version) => version + 1);
}
