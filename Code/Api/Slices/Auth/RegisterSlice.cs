using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacaFlow.Application.Exceptions;
using VacaFlow.Application.Validation;
using VacaFlow.Domain.Entities;
using VacaFlow.Domain.Enums;
using VacaFlow.Domain.Ports;
using VacaFlow.Infrastructure.Persistence;

namespace VacaFlow.Api.Slices.Auth;

/// <summary>
/// US-001 — User Registration. Creates an <see cref="Employee"/> with a BCrypt password hash,
/// establishes the authenticated cookie session (auto sign-in), and returns a safe DTO.
/// FR-AUTH-001/002 · BR-USER-001 · BR-SEC-001.
/// </summary>
public static class RegisterSlice
{
    // Role is nullable so an omitted "role" is a validation error, not a silent default to Employee (BR-USER-001).
    public record RegisterCommand(string FullName, string Email, string Password, Role? Role);

    public record RegisteredUserDto(Guid Id, string FullName, string Email, string Role);

    public class RegisterValidator : IValidator<RegisterCommand>
    {
        public const int MinPasswordLength = 8;

        public ValidationResult Validate(RegisterCommand command)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(command.FullName))
                errors.Add(new("fullName", "Full name is required."));

            if (string.IsNullOrWhiteSpace(command.Email))
                errors.Add(new("email", "Email is required."));
            else if (!IsValidEmail(command.Email))
                errors.Add(new("email", "Email format is invalid."));

            if (string.IsNullOrWhiteSpace(command.Password))
                errors.Add(new("password", "Password is required."));
            else if (command.Password.Length < MinPasswordLength)
                errors.Add(new("password", $"Password must be at least {MinPasswordLength} characters."));

            if (command.Role is null || !Enum.IsDefined(command.Role.Value))
                errors.Add(new("role", "Role must be either Employee or Manager."));

            return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                return new MailAddress(email).Address == email;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", HandleAsync).AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] RegisterCommand command,
        HttpContext httpContext,
        VacaFlowDbContext db,
        IPasswordHasher hasher,
        ValidationPipelineInvoker validation,
        CancellationToken ct)
    {
        validation.Validate(command);

        var email = command.Email.Trim().ToLowerInvariant();

        if (await db.Employees.AnyAsync(e => e.Email == email, ct))
            throw new ConflictException("Email already registered.");

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FullName = command.FullName.Trim(),
            Email = email,
            PasswordHash = hasher.Hash(command.Password),
            Role = command.Role!.Value, // guaranteed non-null by the validator above
            ManagerId = null
        };

        db.Employees.Add(employee);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique-index backstop against a concurrent duplicate registration (AC-003).
            throw new ConflictException("Email already registered.");
        }

        await SignInAsync(httpContext, employee);

        var dto = new RegisteredUserDto(employee.Id, employee.FullName, employee.Email, employee.Role.ToString());
        // No GET-by-id resource exists yet (US-002+), so return 201 without a misleading Location header.
        return Results.Created((string?)null, dto);
    }

    private static Task SignInAsync(HttpContext httpContext, Employee employee)
    {
        var claims = new List<Claim>
        {
            new("sub", employee.Id.ToString()),
            new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new(ClaimTypes.Name, employee.FullName),
            new(ClaimTypes.Email, employee.Email),
            new(ClaimTypes.Role, employee.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
