using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacaFlow.Application.Validation;
using VacaFlow.Domain.Ports;
using VacaFlow.Infrastructure.Persistence;

namespace VacaFlow.Api.Slices.Auth;

/// <summary>
/// US-002 — Login. Verifies credentials against the stored BCrypt hash and establishes the
/// cookie session. Invalid credentials return a generic 401 with no field disclosure (AC-002).
/// </summary>
public static class LoginSlice
{
    public record LoginCommand(string Email, string Password);

    public class LoginValidator : IValidator<LoginCommand>
    {
        public ValidationResult Validate(LoginCommand command)
        {
            var errors = new List<ValidationError>();
            if (string.IsNullOrWhiteSpace(command.Email))
                errors.Add(new("email", "Email is required."));
            if (string.IsNullOrWhiteSpace(command.Password))
                errors.Add(new("password", "Password is required."));
            return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
        }
    }

    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", HandleAsync).AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] LoginCommand command,
        HttpContext httpContext,
        VacaFlowDbContext db,
        IPasswordHasher hasher,
        ValidationPipelineInvoker validation,
        CancellationToken ct)
    {
        validation.Validate(command);

        var email = command.Email.Trim().ToLowerInvariant();
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Email == email, ct);

        // Generic failure — never reveal whether the email exists or the password was wrong (AC-002).
        if (employee is null || !hasher.Verify(command.Password, employee.PasswordHash))
            return Results.Json(new { title = "Invalid email or password.", status = 401 },
                contentType: "application/problem+json", statusCode: StatusCodes.Status401Unauthorized);

        await AuthSession.SignInAsync(httpContext, employee);
        return Results.Ok(AuthSession.ToDto(employee));
    }
}
