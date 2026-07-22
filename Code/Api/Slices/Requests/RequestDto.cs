namespace VacaFlow.Api.Slices.Requests;

/// <summary>
/// Read model for a request. <see cref="EmployeeName"/> is the owner (used by the manager view);
/// <see cref="Decision"/>/<see cref="DecisionComment"/> are populated once a manager has decided.
/// </summary>
public record RequestDto(
    Guid Id,
    Guid OwnerEmployeeId,
    string EmployeeName,
    Guid AbsenceTypeId,
    string AbsenceType,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    string Status,
    string? Decision,
    string? DecisionComment);
