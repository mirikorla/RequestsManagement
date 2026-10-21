using RequestsManagement.Core.Enums;

namespace RequestsManagement.Core.DTOs;

/// <summary>RowVersion is sent to the client as base64 and must be echoed back on update.</summary>
public sealed record RequestDto(
    int Id,
    string Title,
    string OrganizationName,
    RequestStatus Status,
    Priority Priority,
    string? AssignedTo,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string RowVersion);
