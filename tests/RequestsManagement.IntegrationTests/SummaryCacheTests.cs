using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using RequestsManagement.Infrastructure.DAL;
using RequestsManagement.Infrastructure.Services;

namespace RequestsManagement.IntegrationTests;

/// <summary>
/// The race the cache must survive: a status change lands while the counts are being computed.
/// The interleaving is forced deterministically by invalidating from inside the first SQL command.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SummaryCacheTests(ApiFixture fixture)
{
    [Fact]
    public async Task Counts_computed_across_a_status_change_are_not_cached()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var interceptor = new RunBeforeFirstCommand();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options);
        var service = new SummaryService(db, cache, TimeProvider.System);

        // "Another request" changes a status after the count started reading, before it finished.
        interceptor.RunOnce(service.Invalidate);
        var first = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(ApiFixture.SeedCount, first.Total); // the caller still gets an answer
        Assert.False(cache.TryGetValue("requests:summary", out _)); // ...but it is not kept for others

        // Without a concurrent change, the next computation is cached as usual.
        var second = await service.GetSummaryAsync(CancellationToken.None);
        var third = await service.GetSummaryAsync(CancellationToken.None);
        Assert.Same(second, third);
    }

    /// <summary>Runs an action once, right before the next SQL command is sent.</summary>
    private sealed class RunBeforeFirstCommand : DbCommandInterceptor
    {
        private Action? _pending;

        public void RunOnce(Action action) => _pending = action;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Exchange(ref _pending, null)?.Invoke();
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
