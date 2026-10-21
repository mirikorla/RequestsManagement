using Microsoft.EntityFrameworkCore;
using RequestsManagement.Core.DTOs;
using RequestsManagement.Core.Exceptions;
using RequestsManagement.Core.Interfaces;
using RequestsManagement.Infrastructure.DAL;
using RequestsManagement.Infrastructure.Mapping;
using RequestsManagement.Infrastructure.Queries;

namespace RequestsManagement.Infrastructure.Services;

/// <summary>
/// Read side. The query is composed by <see cref="RequestQueryBuilder"/> and executed by SQL Server;
/// only one page of DTO columns is ever materialized in memory.
/// </summary>
public sealed class RequestQueryService(AppDbContext db) : IRequestQueryService
{
    public async Task<PagedResultDto<RequestDto>> SearchAsync(RequestQueryDto query, CancellationToken ct)
    {
        var filtered = db.Requests.AsNoTracking().ApplyFilters(query);

        var total = await filtered.CountAsync(ct);

        var rows = await filtered
            .ApplySort(query)
            .ApplyPage(query)
            .Select(RequestMapping.ToRow)
            .ToListAsync(ct);

        return new PagedResultDto<RequestDto>(rows.Select(r => r.ToDto()).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<RequestDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var row = await db.Requests.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(RequestMapping.ToRow)
            .FirstOrDefaultAsync(ct);

        return row?.ToDto() ?? throw new NotFoundException("Request", id);
    }

    public async Task<IReadOnlyList<StatusHistoryDto>> GetHistoryAsync(int id, CancellationToken ct)
    {
        if (!await db.Requests.AnyAsync(r => r.Id == id, ct))
            throw new NotFoundException("Request", id);

        var rows = await db.StatusHistory.AsNoTracking()
            .Where(h => h.RequestId == id)
            .OrderByDescending(h => h.ChangedAt).ThenByDescending(h => h.Id)
            .ToListAsync(ct);

        return rows.Select(h => h.ToDto()).ToList();
    }
}
