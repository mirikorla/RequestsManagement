using RequestsManagement.Core.Enums;
namespace RequestsManagement.Core.Models;

/// <summary>
/// Allowed status workflow:
///   New        -> InProgress, Waiting
///   InProgress -> Waiting, Completed
///   Waiting    -> InProgress, Completed
///   Completed  -> InProgress (reopen)
/// Staying in the same status is not a transition and is rejected.
/// </summary>
public static class StatusTransitions
{
    private static readonly IReadOnlyDictionary<RequestStatus, RequestStatus[]> Allowed =
        new Dictionary<RequestStatus, RequestStatus[]>
        {
            [RequestStatus.New] = [RequestStatus.InProgress, RequestStatus.Waiting],
            [RequestStatus.InProgress] = [RequestStatus.Waiting, RequestStatus.Completed],
            [RequestStatus.Waiting] = [RequestStatus.InProgress, RequestStatus.Completed],
            [RequestStatus.Completed] = [RequestStatus.InProgress],
        };

    public static bool IsAllowed(RequestStatus from, RequestStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyList<RequestStatus> AllowedFrom(RequestStatus from) =>
        Allowed.TryGetValue(from, out var targets) ? targets : [];
}
