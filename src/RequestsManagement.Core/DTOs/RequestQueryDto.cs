using RequestsManagement.Core.Enums;

namespace RequestsManagement.Core.DTOs;

/// <summary>Query string parameters for GET /api/requests. All filters are optional and combined with AND.</summary>
public sealed class RequestQueryDto
{
    public const int MaxPageSize = 100;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Free text, matched against Title and OrganizationName (contains).</summary>
    public string? Search { get; set; }

    public RequestStatus? Status { get; set; }
    public Priority? Priority { get; set; }

    /// <summary>Prefix match ("starts with"), so it can use the OrganizationName index.</summary>
    public string? OrganizationName { get; set; }

    /// <summary>Exact match.</summary>
    public string? AssignedTo { get; set; }

    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }

    public RequestSortField SortBy { get; set; } = RequestSortField.CreatedAt;
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
}
