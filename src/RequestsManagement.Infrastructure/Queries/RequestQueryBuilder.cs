using RequestsManagement.Core.DTOs;
using RequestsManagement.Core.Enums;
using RequestsManagement.Core.Models;

namespace RequestsManagement.Infrastructure.Queries;

/// <summary>
/// Translates a <see cref="RequestQueryDto"/> into IQueryable operators. It only composes the query;
/// nothing runs until the caller materializes it, so SQL Server does all the filtering and sorting.
/// The filters match the indexes in AppDbContext (see docs/PERFORMANCE.md).
/// </summary>
internal static class RequestQueryBuilder
{
    public static IQueryable<ServiceRequest> ApplyFilters(this IQueryable<ServiceRequest> source, RequestQueryDto q)
    {
        if (q.Status is { } status)
            source = source.Where(r => r.Status == status);

        if (q.Priority is { } priority)
            source = source.Where(r => r.Priority == priority);

        if (!string.IsNullOrWhiteSpace(q.OrganizationName))
        {
            var org = q.OrganizationName.Trim();
            source = source.Where(r => r.OrganizationName.StartsWith(org)); // LIKE 'x%' -> index seek
        }

        if (!string.IsNullOrWhiteSpace(q.AssignedTo))
        {
            var assignee = q.AssignedTo.Trim();
            source = source.Where(r => r.AssignedTo == assignee);
        }

        if (q.CreatedFrom is { } from)
            source = source.Where(r => r.CreatedAt >= from);

        if (q.CreatedTo is { } to)
            source = source.Where(r => r.CreatedAt <= to);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            // LIKE '%x%' cannot seek an index; it is applied after the other (sargable) filters narrow the rows.
            // Known limitation, see docs/PERFORMANCE.md (Full-Text Search is the suggested improvement).
            var term = q.Search.Trim();
            source = source.Where(r => r.Title.Contains(term) || r.OrganizationName.Contains(term));
        }

        return source;
    }

    public static IQueryable<ServiceRequest> ApplySort(this IQueryable<ServiceRequest> source, RequestQueryDto q)
    {
        var desc = q.SortDirection == SortDirection.Desc;

        IOrderedQueryable<ServiceRequest> ordered = q.SortBy switch
        {
            RequestSortField.UpdatedAt => desc ? source.OrderByDescending(r => r.UpdatedAt) : source.OrderBy(r => r.UpdatedAt),
            RequestSortField.Priority => desc ? source.OrderByDescending(r => r.Priority) : source.OrderBy(r => r.Priority),
            RequestSortField.Status => desc ? source.OrderByDescending(r => r.Status) : source.OrderBy(r => r.Status),
            RequestSortField.Title => desc ? source.OrderByDescending(r => r.Title) : source.OrderBy(r => r.Title),
            RequestSortField.OrganizationName => desc ? source.OrderByDescending(r => r.OrganizationName) : source.OrderBy(r => r.OrganizationName),
            _ => desc ? source.OrderByDescending(r => r.CreatedAt) : source.OrderBy(r => r.CreatedAt),
        };

        // Id as a tie-breaker gives a stable, deterministic order so rows don't jump between pages.
        return desc ? ordered.ThenByDescending(r => r.Id) : ordered.ThenBy(r => r.Id);
    }

    public static IQueryable<ServiceRequest> ApplyPage(this IQueryable<ServiceRequest> source, RequestQueryDto q) =>
        source.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize);
}
