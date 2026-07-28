namespace CampaignUserService.Domain.Exceptions;

/// <summary>
/// Thrown when an invariant of the domain model would be violated.
/// Represents an unrecoverable programming error (invalid state transition,
/// missing required data), as opposed to expected business-rule failures
/// which are represented with <c>Result</c>/<c>Error</c>.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
