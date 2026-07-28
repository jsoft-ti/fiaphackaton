namespace DonationService.Domain.Exceptions;

/// <summary>Thrown when an invariant of the domain model would be violated.</summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
