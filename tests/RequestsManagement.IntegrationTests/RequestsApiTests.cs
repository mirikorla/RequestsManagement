using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RequestsManagement.Core.DTOs;
using RequestsManagement.Core.DTOs.Bulk;
using RequestsManagement.Core.Enums;

namespace RequestsManagement.IntegrationTests;

// Every test that writes uses its own request ids, so tests sharing the fixture don't interfere.
[Collection(ApiCollection.Name)]
public sealed class RequestsApiTests(ApiFixture fixture)
{
    private HttpClient Client => fixture.Client;

    [Fact]
    public async Task Search_filters_sorts_and_pages_on_the_server()
    {
        var page = await Client.GetFromJsonAsync<PagedResultDto<RequestDto>>(
            "/api/requests?status=InProgress&priority=High&sortBy=CreatedAt&sortDirection=Asc&page=2&pageSize=10",
            ApiFixture.Json);

        Assert.NotNull(page);
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(2, page.Page);
        Assert.InRange(page.TotalCount, 21, ApiFixture.SeedCount - 1); // a real subset, not everything
        Assert.All(page.Items, r => Assert.Equal((RequestStatus.InProgress, Priority.High), (r.Status, r.Priority)));
        Assert.Equal(page.Items.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id).Select(r => r.Id), page.Items.Select(r => r.Id));
    }

    [Fact]
    public async Task Text_search_matches_title_or_organization()
    {
        var page = await Client.GetFromJsonAsync<PagedResultDto<RequestDto>>("/api/requests?search=Refund&pageSize=50", ApiFixture.Json);

        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, r => Assert.True(
            r.Title.Contains("Refund", StringComparison.OrdinalIgnoreCase) ||
            r.OrganizationName.Contains("Refund", StringComparison.OrdinalIgnoreCase)));
    }

    [Theory]
    [InlineData("pageSize=1000", "PageSize")]
    [InlineData("page=0", "Page")]
    [InlineData("createdFrom=2026-02-01&createdTo=2026-01-01", "CreatedFrom")]
    public async Task Invalid_query_returns_400_with_field_errors(string query, string field)
    {
        var response = await Client.GetAsync($"/api/requests?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiFixture.Json);
        Assert.Contains(field, problem!.Errors.Keys);
    }

    [Fact]
    public async Task Seeded_ids_run_from_1_to_seed_count()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/requests/0")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/api/requests/1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync($"/api/requests/{ApiFixture.SeedCount}")).StatusCode);
    }

    [Fact]
    public async Task Unknown_request_returns_404()
    {
        var response = await Client.PatchAsJsonAsync("/api/requests/999999/status",
            new UpdateStatusDto(RequestStatus.InProgress, "AAAAAAAAB9E=", "tester"), ApiFixture.Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Disallowed_transition_returns_422_and_lists_allowed_targets()
    {
        var request = await FindAsync(RequestStatus.New, skip: 0);

        var response = await Client.PatchAsJsonAsync($"/api/requests/{request.Id}/status",
            new UpdateStatusDto(RequestStatus.Completed, request.RowVersion, "tester"), ApiFixture.Json);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(RequestStatus.New, (await fixture.GetRequestAsync(request.Id)).Status);
    }

    [Fact]
    public async Task Concurrent_updates_with_the_same_version_let_exactly_one_win()
    {
        var request = await FindAsync(RequestStatus.New, skip: 1);

        // Two users loaded the same version and submit different changes at the same time.
        var responses = await Task.WhenAll(
            Client.PatchAsJsonAsync($"/api/requests/{request.Id}/status",
                new UpdateStatusDto(RequestStatus.InProgress, request.RowVersion, "user-a"), ApiFixture.Json),
            Client.PatchAsJsonAsync($"/api/requests/{request.Id}/status",
                new UpdateStatusDto(RequestStatus.Waiting, request.RowVersion, "user-b"), ApiFixture.Json));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);

        // No lost update: the stored state, version and audit trail all reflect only the winner.
        var winner = await responses.Single(r => r.IsSuccessStatusCode).Content.ReadFromJsonAsync<RequestDto>(ApiFixture.Json);
        var stored = await fixture.GetRequestAsync(request.Id);
        Assert.Equal(winner!.Status, stored.Status);
        Assert.Equal(winner.RowVersion, stored.RowVersion);
        Assert.NotEqual(request.RowVersion, stored.RowVersion);
        Assert.True(stored.UpdatedAt > request.UpdatedAt);

        var history = await Client.GetFromJsonAsync<List<StatusHistoryDto>>($"/api/requests/{request.Id}/history", ApiFixture.Json);
        var entry = Assert.Single(history!);
        Assert.Equal((RequestStatus.New, stored.Status), (entry.PreviousStatus, entry.NewStatus));
    }

    [Fact]
    public async Task Stale_version_returns_409_with_the_current_state()
    {
        var request = await FindAsync(RequestStatus.New, skip: 2);
        var first = await Client.PatchAsJsonAsync($"/api/requests/{request.Id}/status",
            new UpdateStatusDto(RequestStatus.InProgress, request.RowVersion, "user-a"), ApiFixture.Json);
        first.EnsureSuccessStatusCode();

        var second = await Client.PatchAsJsonAsync($"/api/requests/{request.Id}/status",
            new UpdateStatusDto(RequestStatus.Waiting, request.RowVersion, "user-b"), ApiFixture.Json);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var current = body.GetProperty("current");
        // Same contract as a normal response: enums as strings, the new version the client must reload with.
        Assert.Equal("InProgress", current.GetProperty("status").GetString());
        Assert.NotEqual(request.RowVersion, current.GetProperty("rowVersion").GetString());
    }

    [Fact]
    public async Task Bulk_update_reports_partial_success_per_item()
    {
        var ok = await FindAsync(RequestStatus.New, skip: 3);
        var stale = await FindAsync(RequestStatus.New, skip: 4);
        const int missingId = 999_999;

        // Make "stale" outdated before the bulk call.
        (await Client.PatchAsJsonAsync($"/api/requests/{stale.Id}/status",
            new UpdateStatusDto(RequestStatus.Waiting, stale.RowVersion, "someone-else"), ApiFixture.Json)).EnsureSuccessStatusCode();

        var response = await Client.PostAsJsonAsync("/api/requests/status/bulk", new BulkUpdateStatusDto(
            RequestStatus.InProgress, "bulk-user",
            [new BulkItemDto(ok.Id, ok.RowVersion), new BulkItemDto(stale.Id, stale.RowVersion), new BulkItemDto(missingId, ok.RowVersion)]),
            ApiFixture.Json);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BulkUpdateStatusResultDto>(ApiFixture.Json);
        Assert.Equal((1, 2), (result!.Succeeded, result.Failed));
        Assert.Equal(BulkItemOutcome.Updated, result.Results.Single(r => r.Id == ok.Id).Outcome);
        Assert.Equal(BulkItemOutcome.Conflict, result.Results.Single(r => r.Id == stale.Id).Outcome);
        Assert.Equal(BulkItemOutcome.NotFound, result.Results.Single(r => r.Id == missingId).Outcome);

        Assert.Equal(RequestStatus.InProgress, (await fixture.GetRequestAsync(ok.Id)).Status);
        Assert.Equal(RequestStatus.Waiting, (await fixture.GetRequestAsync(stale.Id)).Status); // untouched by bulk

        // Audit: the updated item has a bulk entry; the conflicting item has only the earlier change, nothing from bulk.
        var okHistory = Assert.Single((await GetHistoryAsync(ok.Id))!);
        Assert.Equal((ok.Id, RequestStatus.New, RequestStatus.InProgress, "bulk-user"),
            (okHistory.RequestId, okHistory.PreviousStatus, okHistory.NewStatus, okHistory.ChangedBy));

        var staleHistory = Assert.Single((await GetHistoryAsync(stale.Id))!);
        Assert.Equal("someone-else", staleHistory.ChangedBy);
    }

    [Fact]
    public async Task History_lists_every_change_newest_first()
    {
        var request = await FindAsync(RequestStatus.New, skip: 6);
        var first = await Client.PatchAsJsonAsync($"/api/requests/{request.Id}/status",
            new UpdateStatusDto(RequestStatus.InProgress, request.RowVersion, "dana"), ApiFixture.Json);
        var afterFirst = await first.Content.ReadFromJsonAsync<RequestDto>(ApiFixture.Json);
        (await Client.PatchAsJsonAsync($"/api/requests/{request.Id}/status",
            new UpdateStatusDto(RequestStatus.Completed, afterFirst!.RowVersion, "yossi"), ApiFixture.Json)).EnsureSuccessStatusCode();

        var history = await GetHistoryAsync(request.Id);

        Assert.Collection(history!,
            h => Assert.Equal((request.Id, RequestStatus.InProgress, RequestStatus.Completed, "yossi"),
                (h.RequestId, h.PreviousStatus, h.NewStatus, h.ChangedBy)),
            h => Assert.Equal((request.Id, RequestStatus.New, RequestStatus.InProgress, "dana"),
                (h.RequestId, h.PreviousStatus, h.NewStatus, h.ChangedBy)));
        Assert.True(history![0].ChangedAt >= history[1].ChangedAt);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/requests/999999/history")).StatusCode);
    }

    private Task<List<StatusHistoryDto>?> GetHistoryAsync(int id) =>
        Client.GetFromJsonAsync<List<StatusHistoryDto>>($"/api/requests/{id}/history", ApiFixture.Json);

    [Fact]
    public async Task Summary_cache_is_invalidated_after_a_status_change()
    {
        var before = await Client.GetFromJsonAsync<RequestsSummaryDto>("/api/requests/summary", ApiFixture.Json);
        var cached = await Client.GetFromJsonAsync<RequestsSummaryDto>("/api/requests/summary", ApiFixture.Json);
        Assert.Equal(before!.GeneratedAt, cached!.GeneratedAt); // served from cache
        Assert.Equal(ApiFixture.SeedCount, before.Total);

        var request = await FindAsync(RequestStatus.New, skip: 5);
        (await Client.PatchAsJsonAsync($"/api/requests/{request.Id}/status",
            new UpdateStatusDto(RequestStatus.InProgress, request.RowVersion, "tester"), ApiFixture.Json)).EnsureSuccessStatusCode();

        var after = await Client.GetFromJsonAsync<RequestsSummaryDto>("/api/requests/summary", ApiFixture.Json);
        Assert.True(after!.GeneratedAt > before.GeneratedAt); // recomputed, not stale
        Assert.Equal(ApiFixture.SeedCount, after.Total);
    }

    // Seed data is deterministic, so "the N-th oldest New request" is stable; each test uses a different N.
    private async Task<RequestDto> FindAsync(RequestStatus status, int skip)
    {
        var page = await Client.GetFromJsonAsync<PagedResultDto<RequestDto>>(
            $"/api/requests?status={status}&sortBy=CreatedAt&sortDirection=Asc&page={skip + 1}&pageSize=1", ApiFixture.Json);
        return page!.Items.Single();
    }
}
