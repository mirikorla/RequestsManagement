using RequestsManagement.Core.DTOs;
using RequestsManagement.Core.DTOs.Bulk;

namespace RequestsManagement.Core.Interfaces;

/// <summary>Write side: status changes with optimistic concurrency and audit.</summary>
public interface IRequestStatusService
{
    Task<RequestDto> UpdateStatusAsync(int id, UpdateStatusDto request, CancellationToken ct);
    Task<BulkUpdateStatusResultDto> BulkUpdateStatusAsync(BulkUpdateStatusDto request, CancellationToken ct);
}
