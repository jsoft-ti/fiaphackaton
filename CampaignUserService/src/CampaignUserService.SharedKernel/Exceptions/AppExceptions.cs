namespace CampaignUserService.SharedKernel.Exceptions;

/// <summary>
/// Base type for every exception intentionally thrown by the application.
/// The global exception middleware maps these to RFC 7807 ProblemDetails.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }

    protected AppException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public abstract string Code { get; }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string entityName, object key)
        : base($"Entity \"{entityName}\" ({key}) was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }

    public override string Code => "resource_not_found";
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message)
    {
    }

    public override string Code => "conflict";
}

public sealed class ValidationAppException : AppException
{
    public ValidationAppException(IDictionary<string, string[]> errors) : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }

    public override string Code => "validation_error";
}

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message) : base(message)
    {
    }

    public override string Code => "unauthorized";
}

public sealed class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message) : base(message)
    {
    }

    public override string Code => "forbidden";
}

public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message)
    {
    }

    public override string Code => "business_rule_violation";
}
