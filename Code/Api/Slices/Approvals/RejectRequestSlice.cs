using VacaFlow.Domain.Enums;
using VacaFlow.Infrastructure.Persistence;

namespace VacaFlow.Api.Slices.Approvals;

/// <summary>US-007 — Manager rejects a Submitted request (with an optional comment).</summary>
public static class RejectRequestSlice
{
    public record RejectCommand(string? Comment);

    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/requests/{id:guid}/reject",
            (Guid id, RejectCommand? command, HttpContext httpContext, VacaFlowDbContext db, CancellationToken ct)
                => ApprovalHandler.DecideAsync(id, command?.Comment, httpContext, db,
                    RequestAction.Reject, ApprovalDecision.Rejected, ct))
            .RequireAuthorization();
    }
}
