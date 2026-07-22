using VacaFlow.Domain.Enums;

namespace VacaFlow.Domain.Entities;

/// <summary>
/// An absence/vacation request owned by one <see cref="Employee"/>. Its lifecycle
/// (Draft → Submitted → Approved/Rejected/Cancelled) is governed exclusively by
/// <see cref="StateMachine.RequestStateMachine"/>.
/// </summary>
public class AbsenceRequest
{
    public Guid Id { get; set; }
    public required Guid OwnerEmployeeId { get; set; }
    public required Guid AbsenceTypeId { get; set; }
    public required DateOnly StartDate { get; set; }
    public required DateOnly EndDate { get; set; }
    public required string Reason { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Draft;
}
