using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequestsManagement.Core.DTOs;
using RequestsManagement.Core.DTOs.Bulk;
using RequestsManagement.Core.Enums;
using RequestsManagement.Core.Exceptions;
using RequestsManagement.Core.Interfaces;
using RequestsManagement.Core.Models;
using RequestsManagement.Infrastructure.DAL;
using RequestsManagement.Infrastructure.Mapping;

namespace RequestsManagement.Infrastructure.Services;

/// <summary>
/// Write side. Optimistic concurrency is enforced by the database, not the UI:
/// the client's RowVersion is set as the entity's ORIGINAL value, so EF issues
/// <c>UPDATE ... WHERE Id = @id AND RowVersion = @clientVersion</c>. If another user updated the row
/// in between, 0 rows are affected, EF throws DbUpdateConcurrencyException and we return 409.
/// The status change and its audit row are saved in the same SaveChanges, i.e. the same transaction.
/// </summary>
public sealed class RequestStatusService(
    AppDbContext db,
    ISummaryService summary,
    TimeProvider clock,
    ILogger<RequestStatusService> logger) : IRequestStatusService
{
    public async Task<RequestDto> UpdateStatusAsync(int id, UpdateStatusDto request, CancellationToken ct)
    {
        var entity = await db.Requests.FirstOrDefaultAsync(r => r.Id == id, ct)
                     ?? throw new NotFoundException("Request", id);

        var outcome = await TryApplyAsync(entity, request.Status, Convert.FromBase64String(request.RowVersion), request.ChangedBy, ct);

        switch (outcome)
        {
            case StatusChangeOutcome.Conflict:
                logger.LogWarning("Concurrency conflict updating request {RequestId}", id);
                throw new ConcurrencyConflictException(id, await LoadCurrentAsync(id, ct));
            case StatusChangeOutcome.InvalidTransition:
                throw new InvalidStatusTransitionException(id, entity.Status, request.Status);
        }

        summary.Invalidate();
        logger.LogInformation("Request {RequestId} status changed to {Status}", id, entity.Status);
        return entity.ToDto();
    }

    /// <summary>
    /// Partial success (see README): items are independent, so one stale or missing item
    /// does not block the others. Each item is committed in its own transaction, and the
    /// response tells the client exactly which items to reload and retry.
    /// </summary>
    public async Task<BulkUpdateStatusResultDto> BulkUpdateStatusAsync(BulkUpdateStatusDto request, CancellationToken ct)
    {
        var ids = request.Items.Select(i => i.Id).ToList();
        var entities = await db.Requests.Where(r => ids.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct);

        var results = new List<BulkItemResultDto>(request.Items.Count);
        foreach (var item in request.Items)
        {
            if (!entities.TryGetValue(item.Id, out var entity))
            {
                results.Add(new BulkItemResultDto(item.Id, BulkItemOutcome.NotFound, Error: "Request not found."));
                continue;
            }

            var outcome = await TryApplyAsync(entity, request.Status, Convert.FromBase64String(item.RowVersion), request.ChangedBy, ct);
            results.Add(outcome switch
            {
                StatusChangeOutcome.Updated => new BulkItemResultDto(
                    item.Id, BulkItemOutcome.Updated, Convert.ToBase64String(entity.RowVersion)),
                StatusChangeOutcome.Conflict => new BulkItemResultDto(
                    item.Id, BulkItemOutcome.Conflict, Error: "Modified by another user. Reload and retry."),
                _ => new BulkItemResultDto(
                    item.Id, BulkItemOutcome.InvalidTransition, Error: $"Transition {entity.Status} -> {request.Status} is not allowed."),
            });
        }

        var succeeded = results.Count(r => r.Outcome == BulkItemOutcome.Updated);
        if (succeeded > 0)
            summary.Invalidate();

        logger.LogInformation("Bulk status update to {Status}: {Succeeded} succeeded, {Failed} failed",
            request.Status, succeeded, results.Count - succeeded);

        return new BulkUpdateStatusResultDto(succeeded, results.Count - succeeded, results);
    }

    private async Task<StatusChangeOutcome> TryApplyAsync(
        ServiceRequest entity, RequestStatus newStatus, byte[] clientRowVersion, string changedBy, CancellationToken ct)
    {
        // Fast path: the client already holds a stale version, no need to hit the DB.
        if (!entity.RowVersion.AsSpan().SequenceEqual(clientRowVersion))
            return StatusChangeOutcome.Conflict;

        if (!StatusTransitions.IsAllowed(entity.Status, newStatus))
            return StatusChangeOutcome.InvalidTransition;

        var entry = db.Entry(entity);
        entry.Property(r => r.RowVersion).OriginalValue = clientRowVersion;
        var history = entity.ChangeStatus(newStatus, changedBy, clock.GetUtcNow().UtcDateTime);
        var historyEntry = db.Entry(history);

        try
        {
            await db.SaveChangesAsync(ct); // UPDATE Requests + INSERT history, one transaction
            return StatusChangeOutcome.Updated;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else won the race between our read and our write. Undo the local changes so
            // they are not re-sent by a later SaveChanges (relevant for bulk, where the context is reused).
            // Detach through the entries captured before saving: db.Entry() would run DetectChanges on the
            // half-saved graph and throw. The collection is cleaned up only once nothing is tracked.
            historyEntry.State = EntityState.Detached;
            entry.State = EntityState.Detached;
            entity.History.Remove(history);
            return StatusChangeOutcome.Conflict;
        }
    }

    private async Task<RequestDto?> LoadCurrentAsync(int id, CancellationToken ct)
    {
        var current = await db.Requests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        return current?.ToDto();
    }
}
