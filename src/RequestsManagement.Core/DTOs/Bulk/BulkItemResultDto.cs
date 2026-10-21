using RequestsManagement.Core.Enums;
namespace RequestsManagement.Core.DTOs.Bulk;

public sealed record BulkItemResultDto(int Id, BulkItemOutcome Outcome, string? RowVersion = null, string? Error = null);
