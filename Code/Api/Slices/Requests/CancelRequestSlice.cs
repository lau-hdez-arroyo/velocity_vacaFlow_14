using Microsoft.EntityFrameworkCore;
using VacaFlow.Api.Extensions;
using VacaFlow.Application.Exceptions;
using VacaFlow.Domain.Enums;
using VacaFlow.Domain.StateMachine;
using VacaFlow.Infrastructure.Persistence;

namespace VacaFlow.Api.Slices.Requests;

/// <summary>
/// US-006 — Cancel a request. Owner only (BR-REQ-004); transition Draft|Submitted → Cancelled
/// via the state machine (BR-REQ-005); terminal states are rejected with 422 (BR-STATE-001).
/// </summary>
public static class CancelRequestSlice
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/requests/{id:guid}/cancel", HandleAsync).RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext httpContext,
        VacaFlowDbContext db,
        CancellationToken ct)
    {
        var meId = httpContext.User.EmployeeId();

        var request = await db.AbsenceRequests.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Request {id} not found.");

        if (request.OwnerEmployeeId != meId)
            throw new AuthorizationException("You can only cancel your own requests.");

        RequestStateMachine.Transition(request, RequestAction.Cancel);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
