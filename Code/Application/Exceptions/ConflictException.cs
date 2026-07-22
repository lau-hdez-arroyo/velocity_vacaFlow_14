namespace VacaFlow.Application.Exceptions;

/// <summary>
/// Raised when an operation conflicts with existing state (e.g. a duplicate unique value).
/// Mapped to HTTP 409 by the API.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
