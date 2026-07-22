using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VacaFlow.Domain.Entities;

namespace VacaFlow.Api.Slices.Auth;

/// <summary>Safe projection of the authenticated user (never carries the password hash).</summary>
public record CurrentUserDto(Guid Id, string FullName, string Email, string Role);

/// <summary>Shared cookie sign-in used by registration and login.</summary>
internal static class AuthSession
{
    public static Task SignInAsync(HttpContext httpContext, Employee employee)
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

    public static CurrentUserDto ToDto(Employee employee)
        => new(employee.Id, employee.FullName, employee.Email, employee.Role.ToString());
}
