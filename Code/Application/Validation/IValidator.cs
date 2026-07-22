namespace VacaFlow.Application.Validation;

/// <summary>A single field-level validation failure.</summary>
public record ValidationError(string Field, string Message);

/// <summary>Outcome of validating a command/query.</summary>
public sealed class ValidationResult
{
    private ValidationResult(IReadOnlyList<ValidationError> errors) => Errors = errors;

    public IReadOnlyList<ValidationError> Errors { get; }
    public bool IsValid => Errors.Count == 0;

    public static ValidationResult Success() => new(Array.Empty<ValidationError>());
    public static ValidationResult Failure(IReadOnlyList<ValidationError> errors) => new(errors);
}

/// <summary>Validates a command of type <typeparamref name="T"/>. One implementation per command.</summary>
public interface IValidator<in T>
{
    ValidationResult Validate(T instance);
}
