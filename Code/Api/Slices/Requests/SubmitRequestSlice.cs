using Microsoft.EntityFrameworkCore;
using VacaFlow.Api.Extensions;
using VacaFlow.Application.Exceptions;
using VacaFlow.Domain.Enums;
using VacaFlow.Domain.Exceptions;
using VacaFlow.Domain.StateMachine;
using VacaFlow.Infrastructure.Persistence;

namespace VacaFlow.Api.Slices.Requests;

/// <summary>
/// US-005 — Submit a Draft request for review. Owner only (BR-REQ-004); fields re-validated
/// (BR-REQ-006); transition Draft → Submitted via the state machine (invalid state → 422).
/// </summary>
public static class SubmitRequestSlice
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/requests/{id:guid}/submit", HandleAsync).RequireAuthorization();
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
            throw new AuthorizationException("You can only submit your own requests.");

        var errors = RequestFieldRules.Check(request.AbsenceTypeId, request.StartDate, request.EndDate, request.Reason);
        if (errors.Count > 0)
            throw new DomainException(errors[0].Message);

        RequestStateMachine.Transition(request, RequestAction.Submit);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
