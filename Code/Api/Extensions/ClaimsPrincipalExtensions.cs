using System.Security.Claims;
using VacaFlow.Domain.Enums;

namespace VacaFlow.Api.Extensions;

/// <summary>Reads the authenticated identity from the cookie session — the sole source of truth (BR-USER-002).</summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid EmployeeId(this ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Authenticated employee id not found."));

    public static Role Role(this ClaimsPrincipal user)
        => Enum.Parse<Role>(user.FindFirstValue(ClaimTypes.Role)
            ?? throw new UnauthorizedAccessException("Role claim not found."));
}
