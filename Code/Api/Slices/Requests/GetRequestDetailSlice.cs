using Microsoft.EntityFrameworkCore;
using VacaFlow.Api.Extensions;
using VacaFlow.Application.Exceptions;
using VacaFlow.Domain.Enums;
using VacaFlow.Infrastructure.Persistence;

namespace VacaFlow.Api.Slices.Requests;

/// <summary>
/// US-008 — Single request detail, incl. the manager's decision and comment. Visible to the
/// owner or to the assigned manager only.
/// </summary>
public static class GetRequestDetailSlice
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/requests/{id:guid}", HandleAsync).RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(Guid id, HttpContext httpContext, VacaFlowDbContext db, CancellationToken ct)
    {
        var meId = httpContext.User.EmployeeId();
        var role = httpContext.User.Role();

        var request = await db.AbsenceRequests.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Request {id} not found.");

        var owner = await db.Employees.FirstAsync(e => e.Id == request.OwnerEmployeeId, ct);
        var authorized = request.OwnerEmployeeId == meId || (role == Role.Manager && owner.ManagerId == meId);
        if (!authorized)
            throw new AuthorizationException("You are not allowed to view this request.");

        var absenceType = await db.AbsenceTypes.FirstAsync(a => a.Id == request.AbsenceTypeId, ct);
        var approval = await db.Approvals.FirstOrDefaultAsync(a => a.RequestId == id, ct);

        var dto = new RequestDto(
            request.Id, request.OwnerEmployeeId, owner.FullName, request.AbsenceTypeId, absenceType.Name,
            request.StartDate, request.EndDate, request.Reason, request.Status.ToString(),
            approval?.Decision.ToString(), approval?.Comment);

        return Results.Ok(dto);
    }
}
