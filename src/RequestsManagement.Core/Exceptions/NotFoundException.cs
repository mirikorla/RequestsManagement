namespace RequestsManagement.Core.Exceptions;

// Domain exceptions are mapped to HTTP status codes in one place (GlobalExceptionHandler in the Api project).
public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' was not found.");
