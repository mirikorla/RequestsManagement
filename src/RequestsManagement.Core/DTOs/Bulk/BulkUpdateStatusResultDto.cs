namespace RequestsManagement.Core.DTOs.Bulk;

/// <summary>Partial success: every item gets its own outcome; successful items are committed even if others failed.</summary>
public sealed record BulkUpdateStatusResultDto(int Succeeded, int Failed, IReadOnlyList<BulkItemResultDto> Results);
