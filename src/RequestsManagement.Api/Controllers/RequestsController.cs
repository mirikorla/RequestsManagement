using Microsoft.AspNetCore.Mvc;
using RequestsManagement.Core.DTOs;
using RequestsManagement.Core.DTOs.Bulk;
using RequestsManagement.Core.Interfaces;

namespace RequestsManagement.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Produces("application/json")]
public sealed class RequestsController(
    IRequestQueryService queries,
    IRequestStatusService statuses,
    ISummaryService summary) : ControllerBase
{
    /// <summary>Server-side paging, filtering, free-text search and sorting.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<RequestDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResultDto<RequestDto>> Search([FromQuery] RequestQueryDto query, CancellationToken ct) =>
        queries.SearchAsync(query, ct);

    /// <summary>Counts by status and by priority (cached, invalidated on every status change).</summary>
    [HttpGet("summary")]
    [ProducesResponseType<RequestsSummaryDto>(StatusCodes.Status200OK)]
    public Task<RequestsSummaryDto> GetSummary(CancellationToken ct) => summary.GetSummaryAsync(ct);

    [HttpGet("{id:int}")]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<RequestDto> GetById(int id, CancellationToken ct) => queries.GetByIdAsync(id, ct);

    [HttpGet("{id:int}/history")]
    [ProducesResponseType<IReadOnlyList<StatusHistoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<StatusHistoryDto>> GetHistory(int id, CancellationToken ct) =>
        queries.GetHistoryAsync(id, ct);

    /// <summary>
    /// Changes the status. The RowVersion from the last read must be sent back;
    /// if the request was changed since, the response is 409 with the current state.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<RequestDto> UpdateStatus(int id, UpdateStatusDto request, CancellationToken ct) =>
        statuses.UpdateStatusAsync(id, request, ct);

    /// <summary>
    /// Updates up to 100 requests to the same status. Partial success: always 200 when the
    /// payload is valid, with a per-item outcome (Updated / NotFound / Conflict / InvalidTransition).
    /// </summary>
    [HttpPost("status/bulk")]
    [ProducesResponseType<BulkUpdateStatusResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<BulkUpdateStatusResultDto> BulkUpdateStatus(BulkUpdateStatusDto request, CancellationToken ct) =>
        statuses.BulkUpdateStatusAsync(request, ct);
}
