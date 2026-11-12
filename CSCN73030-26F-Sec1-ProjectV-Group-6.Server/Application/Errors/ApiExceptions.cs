/**
 * @file ApiExceptions.cs
 * @brief Exceptions that services throw to produce 400, 404, 409 and 422 responses.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;

// Services throw these; ApiExceptionHandler turns them into RFC 7807 ProblemDetails responses.
// (Written with ordinary constructors rather than primary constructors so Doxygen parses them.)

/// <summary>Base class for errors that map to a specific HTTP status.</summary>
public abstract class ApiException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="statusCode">HTTP status to return.</param>
    /// <param name="title">ProblemDetails title.</param>
    /// <param name="detail">ProblemDetails detail; also the exception message.</param>
    protected ApiException(int statusCode, string title, string detail) : base(detail)
    {
        StatusCode = statusCode;
        Title = title;
    }

    /// <summary>HTTP status code of the response.</summary>
    public int StatusCode { get; }

    /// <summary>Short, fixed title for this kind of error.</summary>
    public string Title { get; }
}

/// <summary>400: the request is malformed (e.g. a required header is missing).</summary>
public class BadRequestException : ApiException
{
    /// <summary>Creates a 400 error.</summary>
    /// <param name="detail">What was wrong with the request.</param>
    public BadRequestException(string detail)
        : base(StatusCodes.Status400BadRequest, "Malformed request", detail)
    {
    }
}

/// <summary>404: the resource in the URL does not exist.</summary>
public class NotFoundException : ApiException
{
    /// <summary>Creates a 404 error with detail "{resource} '{key}' was not found."</summary>
    /// <param name="resource">Kind of resource, e.g. "Staff".</param>
    /// <param name="key">The id from the URL.</param>
    public NotFoundException(string resource, object key)
        : base(StatusCodes.Status404NotFound, "Resource not found", $"{resource} '{key}' was not found.")
    {
    }
}

/// <summary>409: the request conflicts with existing data (duplicate email, idempotency clash, overlapping shift).</summary>
public class ConflictException : ApiException
{
    /// <summary>Creates a 409 error.</summary>
    /// <param name="detail">What the request conflicts with.</param>
    public ConflictException(string detail)
        : base(StatusCodes.Status409Conflict, "Conflict", detail)
    {
    }
}

/// <summary>422: the request is well-formed but breaks one or more field rules.</summary>
public class ValidationFailedException : ApiException
{
    /// <summary>Creates a 422 error listing every invalid field.</summary>
    /// <param name="errors">Field name (camelCase) to its messages.</param>
    public ValidationFailedException(IDictionary<string, string[]> errors)
        : base(StatusCodes.Status422UnprocessableEntity, "Validation failed",
            "One or more fields are invalid. See 'errors' for details.")
    {
        Errors = errors;
    }

    /// <summary>Creates a 422 error for a single field.</summary>
    /// <param name="field">Field name (camelCase).</param>
    /// <param name="message">What is wrong with it.</param>
    public ValidationFailedException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] })
    {
    }

    /// <summary>Field name to its messages; becomes the ProblemDetails 'errors' member.</summary>
    public IDictionary<string, string[]> Errors { get; }
}
