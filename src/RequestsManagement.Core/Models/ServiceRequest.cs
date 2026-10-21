using RequestsManagement.Core.Enums;
using RequestsManagement.Core.Exceptions;

namespace RequestsManagement.Core.Models;

/// <summary>
/// A request ("פנייה") submitted by an organization. Named ServiceRequest to avoid clashing with HTTP "Request".
/// </summary>
public class ServiceRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public RequestStatus Status { get; private set; } = RequestStatus.New;
    public Priority Priority { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; private set; }

    // SQL Server rowversion: changed by the database on every UPDATE, used as the optimistic concurrency token.
    public byte[] RowVersion { get; set; } = [];

    public ICollection<RequestStatusHistory> History { get; } = new List<RequestStatusHistory>();

    public ServiceRequest() { }

    public ServiceRequest(string title, string organizationName, Priority priority, string? assignedTo,
        RequestStatus status, DateTime createdAt, DateTime updatedAt)
    {
        Title = title;
        OrganizationName = organizationName;
        Priority = priority;
        AssignedTo = assignedTo;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Applies a status change and records it in the history. Status, UpdatedAt and the audit row
    /// are changed together so they are always persisted in the same SaveChanges (same transaction).
    /// </summary>
    public RequestStatusHistory ChangeStatus(RequestStatus newStatus, string changedBy, DateTime now)
    {
        if (!StatusTransitions.IsAllowed(Status, newStatus))
            throw new InvalidStatusTransitionException(Id, Status, newStatus);

        var entry = new RequestStatusHistory
        {
            RequestId = Id,
            PreviousStatus = Status,
            NewStatus = newStatus,
            ChangedAt = now,
            ChangedBy = changedBy
        };

        Status = newStatus;
        UpdatedAt = now;
        History.Add(entry);
        return entry;
    }
}
