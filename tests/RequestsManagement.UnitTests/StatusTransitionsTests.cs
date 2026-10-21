using RequestsManagement.Core.Enums;
using RequestsManagement.Core.Exceptions;
using RequestsManagement.Core.Models;

namespace RequestsManagement.UnitTests;

public class StatusTransitionsTests
{
    [Theory]
    [InlineData(RequestStatus.New, RequestStatus.InProgress)]
    [InlineData(RequestStatus.New, RequestStatus.Waiting)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Completed)]
    [InlineData(RequestStatus.Waiting, RequestStatus.InProgress)]
    [InlineData(RequestStatus.Completed, RequestStatus.InProgress)]
    public void Allowed_transitions_are_accepted(RequestStatus from, RequestStatus to) =>
        Assert.True(StatusTransitions.IsAllowed(from, to));

    [Theory]
    [InlineData(RequestStatus.New, RequestStatus.Completed)]
    [InlineData(RequestStatus.Completed, RequestStatus.New)]
    [InlineData(RequestStatus.InProgress, RequestStatus.New)]
    [InlineData(RequestStatus.Waiting, RequestStatus.Waiting)]
    [InlineData(RequestStatus.New, (RequestStatus)99)]
    public void Disallowed_transitions_are_rejected(RequestStatus from, RequestStatus to) =>
        Assert.False(StatusTransitions.IsAllowed(from, to));

    [Fact]
    public void ChangeStatus_updates_status_timestamp_and_records_history_together()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var request = new ServiceRequest("Title", "Org", Priority.High, "agent01", RequestStatus.New, created, created);
        var now = created.AddHours(5);

        var entry = request.ChangeStatus(RequestStatus.InProgress, "dana", now);

        Assert.Equal(RequestStatus.InProgress, request.Status);
        Assert.Equal(now, request.UpdatedAt);
        Assert.Same(entry, Assert.Single(request.History));
        Assert.Equal((RequestStatus.New, RequestStatus.InProgress, "dana", now),
            (entry.PreviousStatus, entry.NewStatus, entry.ChangedBy, entry.ChangedAt));
    }

    [Fact]
    public void ChangeStatus_with_invalid_transition_throws_and_leaves_entity_unchanged()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var request = new ServiceRequest("Title", "Org", Priority.Low, null, RequestStatus.New, created, created);

        Assert.Throws<InvalidStatusTransitionException>(() =>
            request.ChangeStatus(RequestStatus.Completed, "dana", created.AddHours(1)));

        Assert.Equal(RequestStatus.New, request.Status);
        Assert.Equal(created, request.UpdatedAt);
        Assert.Empty(request.History);
    }
}
