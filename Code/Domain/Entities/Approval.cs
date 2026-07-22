using VacaFlow.Domain.Enums;

namespace VacaFlow.Domain.Entities;

/// <summary>
/// The single, immutable record of a manager's decision on a request. Exactly one per
/// request (unique <see cref="RequestId"/>), created atomically with the status transition
/// (BR-APPR-001). The approver identity is always server-derived (BR-APPR-002).
/// </summary>
public class Approval
{
    public Guid Id { get; set; }
    public required Guid RequestId { get; set; }
    public required Guid DecidedByEmployeeId { get; set; }
    public required ApprovalDecision Decision { get; set; }
    public required DateTimeOffset DecidedAt { get; set; }
    public string? Comment { get; set; }
}
