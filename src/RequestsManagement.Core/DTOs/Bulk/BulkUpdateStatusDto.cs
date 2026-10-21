using RequestsManagement.Core.Enums;

namespace RequestsManagement.Core.DTOs.Bulk;

/// <summary>Moves up to 100 requests to the same target status. Each item carries its own RowVersion.</summary>
public sealed record BulkUpdateStatusDto(RequestStatus Status, string ChangedBy, IReadOnlyList<BulkItemDto> Items)
{
    public const int MaxItems = 100;
}
