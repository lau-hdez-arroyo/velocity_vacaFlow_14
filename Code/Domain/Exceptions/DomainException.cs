namespace VacaFlow.Domain.Exceptions;

/// <summary>
/// Raised when a business/domain invariant is violated. Mapped to HTTP 422 by the API.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
