using VacaFlow.Application.Validation;

namespace VacaFlow.Application.Exceptions;

/// <summary>
/// Raised by the validation pipeline when a command fails validation.
/// Carries field-level errors. Mapped to HTTP 400 by the API.
/// </summary>
public class ValidationException : Exception
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationException(IReadOnlyList<ValidationError> errors)
        : base("One or more validation errors occurred.")
        => Errors = errors;
}
