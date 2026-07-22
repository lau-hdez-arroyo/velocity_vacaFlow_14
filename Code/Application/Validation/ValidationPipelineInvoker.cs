using VacaFlow.Application.Exceptions;

namespace VacaFlow.Application.Validation;

/// <summary>
/// Runs the registered <see cref="IValidator{T}"/> for a command before its handler executes.
/// A slice handler calls <see cref="Validate{T}"/> at the top of its happy path; if the command
/// is invalid a <see cref="ValidationException"/> is thrown and mapped to HTTP 400.
/// </summary>
public class ValidationPipelineInvoker
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationPipelineInvoker(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public void Validate<T>(T instance)
    {
        if (_serviceProvider.GetService(typeof(IValidator<T>)) is not IValidator<T> validator)
            return;

        var result = validator.Validate(instance);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
    }
}
