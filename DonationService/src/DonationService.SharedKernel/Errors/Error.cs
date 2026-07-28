namespace DonationService.SharedKernel.Errors;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Dependency,
    Failure
}

/// <summary>Represents an expected business-rule failure (as opposed to an exceptional/unhandled error).</summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error None => new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>An upstream/external dependency (e.g. CampaignService) failed or was unreachable.</summary>
    public static Error Dependency(string code, string message) => new(code, message, ErrorType.Dependency);

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
