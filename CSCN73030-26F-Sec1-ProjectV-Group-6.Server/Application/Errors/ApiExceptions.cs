namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;

// Services throw these; ApiExceptionHandler turns them into RFC 7807 ProblemDetails responses.

/// <summary>Base class for errors that map to a specific HTTP status.</summary>
public abstract class ApiException(int statusCode, string title, string detail) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

/// <summary>400: the request is malformed (e.g. a required header is missing).</summary>
public class BadRequestException(string detail)
    : ApiException(StatusCodes.Status400BadRequest, "Malformed request", detail);

/// <summary>404: the resource in the URL does not exist.</summary>
public class NotFoundException(string resource, object key)
    : ApiException(StatusCodes.Status404NotFound, "Resource not found", $"{resource} '{key}' was not found.");

/// <summary>409: the request conflicts with existing data (duplicate email, idempotency clash, overlapping shift).</summary>
public class ConflictException(string detail)
    : ApiException(StatusCodes.Status409Conflict, "Conflict", detail);

/// <summary>422: the request is well-formed but breaks one or more field rules.</summary>
public class ValidationFailedException(IDictionary<string, string[]> errors)
    : ApiException(StatusCodes.Status422UnprocessableEntity, "Validation failed",
        "One or more fields are invalid. See 'errors' for details.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public ValidationFailedException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] })
    {
    }
}
