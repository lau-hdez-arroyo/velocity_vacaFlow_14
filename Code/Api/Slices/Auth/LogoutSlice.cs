using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace VacaFlow.Api.Slices.Auth;

/// <summary>
/// US-002 — Logout. Terminates the server-side cookie session; the old cookie no longer
/// authorizes any request (AC-003, NFR-SEC-005).
/// </summary>
public static class LogoutSlice
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization();
    }
}
