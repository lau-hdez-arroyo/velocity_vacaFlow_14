using VacaFlow.Domain.Enums;
using VacaFlow.Infrastructure.Persistence;

namespace VacaFlow.Api.Slices.Approvals;

/// <summary>US-007 — Manager approves a Submitted request (with an optional comment).</summary>
public static class ApproveRequestSlice
{
    public record ApproveCommand(string? Comment);

    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/requests/{id:guid}/approve",
            (Guid id, ApproveCommand? command, HttpContext httpContext, VacaFlowDbContext db, CancellationToken ct)
                => ApprovalHandler.DecideAsync(id, command?.Comment, httpContext, db,
                    RequestAction.Approve, ApprovalDecision.Approved, ct))
            .RequireAuthorization();
    }
}
