using Microsoft.EntityFrameworkCore;
using VacaFlow.Api.Extensions;
using VacaFlow.Domain.Enums;
using VacaFlow.Infrastructure.Persistence;

namespace VacaFlow.Api.Slices.Requests;

/// <summary>
/// US-008 / US-007 — Role-scoped request list (tech-docs §8.5). Employee → own requests, all
/// statuses, with the manager's decision + comment when present; Manager → Submitted requests of
/// their assigned employees. Empty result → [] (US-008 AC-004).
/// </summary>
public static class GetRequestsSlice
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/requests", HandleAsync).RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(HttpContext httpContext, VacaFlowDbContext db, CancellationToken ct)
    {
        var meId = httpContext.User.EmployeeId();
        var role = httpContext.User.Role();

        var query = db.AbsenceRequests.AsQueryable();
        query = role == Role.Manager
            ? query.Where(r => r.Status == RequestStatus.Submitted
                && db.Employees.Any(e => e.Id == r.OwnerEmployeeId && e.ManagerId == meId))
            : query.Where(r => r.OwnerEmployeeId == meId);

        var rows = await (
            from r in query
            join owner in db.Employees on r.OwnerEmployeeId equals owner.Id
            join at in db.AbsenceTypes on r.AbsenceTypeId equals at.Id
            join ap in db.Approvals on r.Id equals ap.RequestId into aps
            from ap in aps.DefaultIfEmpty()
            orderby r.StartDate descending
            select new Row(
                r.Id, r.OwnerEmployeeId, owner.FullName, r.AbsenceTypeId, at.Name,
                r.StartDate, r.EndDate, r.Reason, r.Status,
                ap != null ? ap.Decision : (ApprovalDecision?)null,
                ap != null ? ap.Comment : null))
            .ToListAsync(ct);

        var items = rows.Select(x => new RequestDto(
            x.Id, x.OwnerEmployeeId, x.EmployeeName, x.AbsenceTypeId, x.AbsenceType,
            x.StartDate, x.EndDate, x.Reason, x.Status.ToString(),
            x.Decision?.ToString(), x.DecisionComment)).ToList();

        return Results.Ok(items);
    }

    private record Row(
        Guid Id, Guid OwnerEmployeeId, string EmployeeName, Guid AbsenceTypeId, string AbsenceType,
        DateOnly StartDate, DateOnly EndDate, string Reason, RequestStatus Status,
        ApprovalDecision? Decision, string? DecisionComment);
}
