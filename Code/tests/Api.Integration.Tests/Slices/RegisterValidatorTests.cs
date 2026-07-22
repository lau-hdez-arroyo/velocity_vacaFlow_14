using VacaFlow.Domain.Enums;
using static VacaFlow.Api.Slices.Auth.RegisterSlice;

namespace Api.Integration.Tests.Slices;

/// <summary>Unit tests for the registration validator — field-level rules (AC-005).</summary>
public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    private static RegisterCommand Valid() => new("Jane Doe", "jane@example.com", "Passw0rd!", Role.Employee);

    [Fact]
    public void Valid_command_passes()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_full_name_fails(string fullName)
    {
        var result = _validator.Validate(Valid() with { FullName = fullName });
        Assert.Contains(result.Errors, e => e.Field == "fullName");
    }

    [Fact]
    public void Missing_email_fails()
    {
        var result = _validator.Validate(Valid() with { Email = "" });
        Assert.Contains(result.Errors, e => e.Field == "email");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at.example.com")]
    public void Invalid_email_format_fails(string email)
    {
        var result = _validator.Validate(Valid() with { Email = email });
        Assert.Contains(result.Errors, e => e.Field == "email");
    }

    [Fact]
    public void Missing_password_fails()
    {
        var result = _validator.Validate(Valid() with { Password = "" });
        Assert.Contains(result.Errors, e => e.Field == "password");
    }

    [Fact]
    public void Short_password_fails()
    {
        var result = _validator.Validate(Valid() with { Password = "short" });
        Assert.Contains(result.Errors, e => e.Field == "password");
    }

    [Fact]
    public void Undefined_role_fails()
    {
        var result = _validator.Validate(Valid() with { Role = (Role)99 });
        Assert.Contains(result.Errors, e => e.Field == "role");
    }

    [Fact]
    public void Missing_role_fails()
    {
        var result = _validator.Validate(Valid() with { Role = null });
        Assert.Contains(result.Errors, e => e.Field == "role");
    }
}
