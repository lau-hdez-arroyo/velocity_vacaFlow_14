using Microsoft.AspNetCore.Antiforgery;

namespace VacaFlow.Api.Slices.Security;

/// <summary>
/// Issues the anti-forgery request token to the SPA. The frontend calls this on load to obtain
/// the JS-readable <c>XSRF-TOKEN</c> cookie, then echoes it in the <c>X-XSRF-TOKEN</c> header on
/// state-changing requests.
/// </summary>
public static class AntiforgeryTokenSlice
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/antiforgery/token", (HttpContext httpContext, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(httpContext);
            httpContext.Response.Cookies.Append(
                "XSRF-TOKEN",
                tokens.RequestToken!,
                new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax });
            return Results.NoContent();
        }).AllowAnonymous();
    }
}
