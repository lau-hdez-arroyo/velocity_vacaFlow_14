using Microsoft.Extensions.DependencyInjection;
using VacaFlow.Application.Exceptions;
using VacaFlow.Application.Validation;

namespace Application.Tests.Validation;

public class ValidationPipelineInvokerTests
{
    private record Sample(string Name);

    private sealed class SampleValidator : IValidator<Sample>
    {
        public ValidationResult Validate(Sample instance)
            => string.IsNullOrWhiteSpace(instance.Name)
                ? ValidationResult.Failure(new[] { new ValidationError("name", "Name is required.") })
                : ValidationResult.Success();
    }

    private static ValidationPipelineInvoker InvokerWithValidator()
    {
        var provider = new ServiceCollection()
            .AddScoped<IValidator<Sample>, SampleValidator>()
            .BuildServiceProvider();
        return new ValidationPipelineInvoker(provider);
    }

    [Fact]
    public void Throws_ValidationException_carrying_field_errors_when_invalid()
    {
        var invoker = InvokerWithValidator();

        var ex = Assert.Throws<ValidationException>(() => invoker.Validate(new Sample("")));
        Assert.Contains(ex.Errors, e => e.Field == "name");
    }

    [Fact]
    public void Does_not_throw_when_valid()
    {
        var invoker = InvokerWithValidator();

        invoker.Validate(new Sample("ok"));
    }

    [Fact]
    public void Is_a_no_op_when_no_validator_is_registered()
    {
        var invoker = new ValidationPipelineInvoker(new ServiceCollection().BuildServiceProvider());

        invoker.Validate(new Sample(""));
    }
}
