using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Torvin.Tests;

// Prije prvog snimanja konteksta jednom izvrši akciju (npr. istu operaciju kroz drugu konekciju). Tako se dvije
// istovremene obrade prave deterministički, bez utrke niti
internal sealed class BeforeFirstSave(Func<Task> action) : SaveChangesInterceptor
{
    private bool _done;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!_done)
        {
            _done = true;
            await action();
        }

        return result;
    }
}
