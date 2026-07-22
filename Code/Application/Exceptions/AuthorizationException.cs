namespace VacaFlow.Application.Exceptions;

/// <summary>
/// Raised when an authenticated user is not permitted to perform an operation
/// (ownership, role, or assignment failure). Mapped to HTTP 403 by the API.
/// </summary>
public class AuthorizationException : Exception
{
    public AuthorizationException(string message) : base(message) { }
}
