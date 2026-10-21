using RequestsManagement.Core.DTOs;

namespace RequestsManagement.Core.Interfaces;

/// <summary>Read side: paged search, a single request, status history.</summary>
public interface IRequestQueryService
{
    Task<PagedResultDto<RequestDto>> SearchAsync(RequestQueryDto query, CancellationToken ct);
    Task<RequestDto> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<StatusHistoryDto>> GetHistoryAsync(int id, CancellationToken ct);
}
