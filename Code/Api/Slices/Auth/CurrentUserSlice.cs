using System.Security.Claims;
using VacaFlow.Api.Extensions;

namespace VacaFlow.Api.Slices.Auth;

/// <summary>
/// US-002 — Current user. Returns the authenticated identity derived exclusively from the
/// session (AC-004). Unauthenticated callers get 401 via the cookie auth events (AC-005).
/// </summary>
public static class CurrentUserSlice
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", (HttpContext httpContext) =>
        {
            var user = httpContext.User;
            var dto = new CurrentUserDto(
                user.EmployeeId(),
                user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                user.Role().ToString());
            return Results.Ok(dto);
        }).RequireAuthorization();
    }
}
