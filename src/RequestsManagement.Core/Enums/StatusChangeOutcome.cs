namespace RequestsManagement.Core.Enums;

/// <summary>
/// Result of trying to change the status of one loaded request. The single update turns it into
/// an HTTP error; the bulk update turns it into a per-item <see cref="BulkItemOutcome"/>.
/// </summary>
public enum StatusChangeOutcome
{
    Updated,
    Conflict,
    InvalidTransition
}
