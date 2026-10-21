using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RequestsManagement.Core.Exceptions;
using RequestsManagement.Core.Models;

namespace RequestsManagement.Api.ErrorHandling;

/// <summary>
/// Single place that maps exceptions to RFC 7807 ProblemDetails responses.
/// Unexpected errors are logged with full detail but the client only gets a generic message.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        if (exception is OperationCanceledException && http.RequestAborted.IsCancellationRequested)
        {
            // The client went away (e.g. Angular switchMap cancelled the call). Nothing to return.
            logger.LogDebug("Request {Path} was cancelled by the client", http.Request.Path);
            http.Response.StatusCode = 499;
            return true;
        }

        var problem = exception switch
        {
            ValidationException ve => new ValidationProblemDetails(
                ve.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            },
            NotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found",
                Detail = exception.Message
            },
            InvalidStatusTransitionException ite => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Invalid status transition",
                Detail = exception.Message,
                Extensions = { ["allowedTransitions"] = StatusTransitions.AllowedFrom(ite.From) }
            },
            ConcurrencyConflictException cce => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency conflict",
                Detail = exception.Message,
                Extensions = { ["current"] = cce.Current }
            },
            _ => null
        };

        if (problem is null)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", http.Request.Method, http.Request.Path);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            };
        }

        http.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
