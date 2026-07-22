using Microsoft.EntityFrameworkCore;
using VacaFlow.Infrastructure.Persistence;

namespace VacaFlow.Api.Slices.Requests;

/// <summary>Seeded absence-type catalog for the request form (BR-DATA-001). US-003 AC-002.</summary>
public static class GetAbsenceTypesSlice
{
    public record AbsenceTypeDto(Guid Id, string Name);

    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/absence-types", async (VacaFlowDbContext db, CancellationToken ct) =>
        {
            var types = await db.AbsenceTypes
                .OrderBy(a => a.Name)
                .Select(a => new AbsenceTypeDto(a.Id, a.Name))
                .ToListAsync(ct);
            return Results.Ok(types);
        }).RequireAuthorization();
    }
}
