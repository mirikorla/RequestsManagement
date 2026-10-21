using System.Linq.Expressions;
using RequestsManagement.Core.DTOs;
using RequestsManagement.Core.Enums;
using RequestsManagement.Core.Models;

namespace RequestsManagement.Infrastructure.Mapping;

/// <summary>The only place that turns entities into DTOs. Entities never leave the Infrastructure layer.</summary>
internal static class RequestMapping
{
    // Used inside IQueryable.Select so EF translates it to a SELECT of only the needed columns.
    // RowVersion is converted to base64 after materialization (ToDto), because SQL can't do that.
    public static readonly Expression<Func<ServiceRequest, RequestRow>> ToRow = r => new RequestRow(
        r.Id, r.Title, r.OrganizationName, r.Status, r.Priority, r.AssignedTo, r.CreatedAt, r.UpdatedAt, r.RowVersion);

    // The same projection for an entity that is already in memory, so the field list exists only once.
    private static readonly Func<ServiceRequest, RequestRow> ToRowInMemory = ToRow.Compile();

    public static RequestDto ToDto(this RequestRow r) => new(
        r.Id, r.Title, r.OrganizationName, r.Status, r.Priority, r.AssignedTo,
        AsUtc(r.CreatedAt), AsUtc(r.UpdatedAt), Convert.ToBase64String(r.RowVersion));

    public static RequestDto ToDto(this ServiceRequest r) => ToRowInMemory(r).ToDto();

    public static StatusHistoryDto ToDto(this RequestStatusHistory h) => new(
        h.Id, h.RequestId, h.PreviousStatus, h.NewStatus, AsUtc(h.ChangedAt), h.ChangedBy);

    // All timestamps are stored as UTC; mark them so JSON serializes them with a "Z" suffix.
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    internal sealed record RequestRow(
        int Id, string Title, string OrganizationName, RequestStatus Status, Priority Priority,
        string? AssignedTo, DateTime CreatedAt, DateTime UpdatedAt, byte[] RowVersion);
}
