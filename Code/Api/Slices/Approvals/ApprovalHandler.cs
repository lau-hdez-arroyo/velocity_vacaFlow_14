using Microsoft.EntityFrameworkCore;
using VacaFlow.Api.Extensions;
using VacaFlow.Application.Exceptions;
using VacaFlow.Domain.Entities;
using VacaFlow.Domain.Enums;
using VacaFlow.Domain.StateMachine;
using VacaFlow.Infrastructure.Persistence;

namespace VacaFlow.Api.Slices.Approvals;

/// <summary>
/// US-007 — shared approve/reject logic. Runs every authorization check unconditionally before the
/// domain transition (tech-docs §6.1.3), then writes the status change and the single Approval
/// record in one atomic commit (BR-APPR-001). Approver identity is always session-derived (BR-APPR-002).
/// </summary>
internal static class ApprovalHandler
{
    public static async Task<IResult> DecideAsync(
        Guid requestId,
        string? comment,
        HttpContext httpContext,
        VacaFlowDbContext db,
        RequestAction action,
        ApprovalDecision decision,
        CancellationToken ct)
    {
        var managerId = httpContext.User.EmployeeId();

        var manager = await db.Employees.FirstOrDefaultAsync(e => e.Id == managerId, ct)
            ?? throw new AuthorizationException("Manager account not found.");
        if (manager.Role != Role.Manager) // BR-MGR-002
            throw new AuthorizationException("Only users with the Manager role can approve or reject requests.");

        var request = await db.AbsenceRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new NotFoundException($"Request {requestId} not found.");
        var owner = await db.Employees.FirstAsync(e => e.Id == request.OwnerEmployeeId, ct);

        if (owner.ManagerId != managerId) // BR-MGR-003
            throw new AuthorizationException("You are not the assigned manager for this request.");
        if (request.OwnerEmployeeId == managerId) // BR-MGR-004
            throw new AuthorizationException("A manager cannot approve or reject their own request.");

        if (await db.Approvals.AnyAsync(a => a.RequestId == requestId, ct)) // BR-APPR-001
            throw new ConflictException("This request has already been decided.");

        RequestStateMachine.Transition(request, action); // requires Submitted (BR-MGR-001) → else 422

        db.Approvals.Add(new Approval
        {
            Id = Guid.NewGuid(),
            RequestId = request.Id,
            DecidedByEmployeeId = managerId,
            Decision = decision,
            DecidedAt = DateTimeOffset.UtcNow,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
        });

        try
        {
            await db.SaveChangesAsync(ct); // atomic: status transition + Approval record
        }
        catch (DbUpdateException)
        {
            // Unique index on Approval.RequestId — concurrent second decision (BR-APPR-001 backstop).
            throw new ConflictException("This request has already been decided.");
        }

        return Results.NoContent();
    }
}
