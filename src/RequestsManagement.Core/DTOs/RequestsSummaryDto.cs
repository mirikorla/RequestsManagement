using RequestsManagement.Core.Enums;

namespace RequestsManagement.Core.DTOs;

public sealed record RequestsSummaryDto(
    int Total,
    IReadOnlyDictionary<RequestStatus, int> ByStatus,
    IReadOnlyDictionary<Priority, int> ByPriority,
    DateTime GeneratedAt);
