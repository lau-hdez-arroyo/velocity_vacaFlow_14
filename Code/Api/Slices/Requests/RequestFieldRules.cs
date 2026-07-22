using VacaFlow.Application.Validation;

namespace VacaFlow.Api.Slices.Requests;

/// <summary>
/// Shared field-level rules for absence requests (BR-REQ-001/002, BR-FIELD-001). Reused by the
/// create/edit validators and re-checked at submission (BR-REQ-006). Dates are evaluated against
/// the server's current date, never a client value.
/// </summary>
internal static class RequestFieldRules
{
    public const int MaxReasonLength = 500;

    public static List<ValidationError> Check(Guid absenceTypeId, DateOnly startDate, DateOnly endDate, string? reason)
    {
        var errors = new List<ValidationError>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        if (absenceTypeId == Guid.Empty)
            errors.Add(new("absenceTypeId", "Absence type is required."));

        if (startDate < today)
            errors.Add(new("startDate", "Start date cannot be in the past."));

        if (endDate < startDate)
            errors.Add(new("endDate", "End date must be on or after the start date."));

        if (string.IsNullOrWhiteSpace(reason))
            errors.Add(new("reason", "A reason is required."));
        else if (reason.Length > MaxReasonLength)
            errors.Add(new("reason", $"Reason must not exceed {MaxReasonLength} characters."));

        return errors;
    }
}
