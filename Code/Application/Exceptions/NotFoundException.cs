namespace VacaFlow.Application.Exceptions;

/// <summary>Raised when a requested resource does not exist. Mapped to HTTP 404 by the API.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
