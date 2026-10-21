using RequestsManagement.Core.DTOs;

namespace RequestsManagement.Core.Interfaces;

/// <summary>Cached aggregations. Invalidate() must be called after every successful status change.</summary>
public interface ISummaryService
{
    Task<RequestsSummaryDto> GetSummaryAsync(CancellationToken ct);
    void Invalidate();
}
