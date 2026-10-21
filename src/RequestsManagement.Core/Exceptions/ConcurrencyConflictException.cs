using RequestsManagement.Core.DTOs;

namespace RequestsManagement.Core.Exceptions;

public sealed class ConcurrencyConflictException(int requestId, RequestDto? current)
    : Exception($"Request {requestId} was modified by another user. Reload and try again.")
{
    /// <summary>The latest state, so the client can show it without another round-trip. Null if it was deleted.</summary>
    public RequestDto? Current { get; } = current;
}
