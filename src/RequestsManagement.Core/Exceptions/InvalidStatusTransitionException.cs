using RequestsManagement.Core.Enums;

namespace RequestsManagement.Core.Exceptions;

public sealed class InvalidStatusTransitionException(int requestId, RequestStatus from, RequestStatus to)
    : Exception($"Status transition {from} -> {to} is not allowed for request {requestId}.")
{
    public RequestStatus From { get; } = from;
    public RequestStatus To { get; } = to;
}
