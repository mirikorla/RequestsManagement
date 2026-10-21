using RequestsManagement.Core.Enums;

namespace RequestsManagement.Core.DTOs;

public sealed record UpdateStatusDto(RequestStatus Status, string RowVersion, string ChangedBy);
