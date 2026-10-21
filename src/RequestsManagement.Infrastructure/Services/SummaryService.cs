using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RequestsManagement.Core.DTOs;
using RequestsManagement.Core.Enums;
using RequestsManagement.Core.Interfaces;
using RequestsManagement.Infrastructure.DAL;

namespace RequestsManagement.Infrastructure.Services;

/// <summary>
/// Dashboard aggregations (counts by status and by priority).
/// Cached because: it is requested on every page load by every user, it scans the whole table
/// (GROUP BY over 100K rows), and it changes only when a status changes, which we know about.
/// Invalidation: every successful status update calls Invalidate(). Expiration is a safety net
/// for changes made outside the API (e.g. re-seeding or direct DB edits).
/// </summary>
public sealed class SummaryService(AppDbContext db, IMemoryCache cache, TimeProvider clock) : ISummaryService
{
    private const string CacheKey = "requests:summary";
    private static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);

    // Guards against caching a stale result: if a status changes while the counts are being computed,
    // Invalidate() runs before our Set and the outdated counts would stay cached until they expire.
    // Every Invalidate() bumps the generation; a result is cached only if the generation it started with
    // is still current. Static because the guarded IMemoryCache is a process-wide singleton too.
    private static readonly Lock Sync = new();
    private static long _generation;

    public async Task<RequestsSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out RequestsSummaryDto? cached) && cached is not null)
            return cached;

        long startedAt;
        lock (Sync) startedAt = _generation;

        var byStatus = await db.Requests.AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var byPriority = await db.Requests.AsNoTracking()
            .GroupBy(r => r.Priority)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        // Always return every enum member, so the client doesn't have to handle missing keys.
        var summary = new RequestsSummaryDto(
            byStatus.Values.Sum(),
            Enum.GetValues<RequestStatus>().ToDictionary(s => s, s => byStatus.GetValueOrDefault(s)),
            Enum.GetValues<Priority>().ToDictionary(p => p, p => byPriority.GetValueOrDefault(p)),
            clock.GetUtcNow().UtcDateTime);

        lock (Sync)
        {
            // Still returned to this caller (it was correct when read), just not kept for others.
            if (_generation == startedAt)
                cache.Set(CacheKey, summary, Expiration);
        }
        return summary;
    }

    public void Invalidate()
    {
        lock (Sync)
        {
            _generation++;
            cache.Remove(CacheKey);
        }
    }
}
