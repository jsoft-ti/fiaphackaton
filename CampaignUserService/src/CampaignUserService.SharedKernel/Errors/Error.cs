namespace CampaignUserService.SharedKernel.Errors;

/// <summary>
/// Category used by the API layer to translate a domain/application error
/// into the correct HTTP status code (RFC 7807 ProblemDetails).
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Failure
}

/// <summary>
/// Represents a business error that is not an exceptional/unexpected condition.
/// Used together with <see cref="Result"/> / <see cref="Result{TValue}"/>.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error None => new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
