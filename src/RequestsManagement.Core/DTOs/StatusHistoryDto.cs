using RequestsManagement.Core.Enums;

namespace RequestsManagement.Core.DTOs;

public sealed record StatusHistoryDto(
    long Id,
    int RequestId,
    RequestStatus PreviousStatus,
    RequestStatus NewStatus,
    DateTime ChangedAt,
    string ChangedBy);
