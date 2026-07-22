using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacaFlow.Api.Extensions;
using VacaFlow.Application.Exceptions;
using VacaFlow.Application.Validation;
using VacaFlow.Domain.Entities;
using VacaFlow.Domain.Enums;
using VacaFlow.Infrastructure.Persistence;

namespace VacaFlow.Api.Slices.Requests;

/// <summary>
/// US-003 — Create a Draft request. Owner is read from the session (BR-USER-002); dates/reason
/// validated (BR-REQ-001/002, BR-FIELD-001); the absence type must exist in the seeded catalog.
/// </summary>
public static class CreateRequestSlice
{
    public record CreateRequestCommand(Guid AbsenceTypeId, DateOnly StartDate, DateOnly EndDate, string Reason);

    public class CreateRequestValidator : IValidator<CreateRequestCommand>
    {
        public ValidationResult Validate(CreateRequestCommand c)
        {
            var errors = RequestFieldRules.Check(c.AbsenceTypeId, c.StartDate, c.EndDate, c.Reason);
            return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
        }
    }

    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/requests", HandleAsync).RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] CreateRequestCommand command,
        HttpContext httpContext,
        VacaFlowDbContext db,
        ValidationPipelineInvoker validation,
        CancellationToken ct)
    {
        validation.Validate(command);

        if (!await db.AbsenceTypes.AnyAsync(a => a.Id == command.AbsenceTypeId, ct))
            throw new NotFoundException("The selected absence type does not exist.");

        var request = new AbsenceRequest
        {
            Id = Guid.NewGuid(),
            OwnerEmployeeId = httpContext.User.EmployeeId(),
            AbsenceTypeId = command.AbsenceTypeId,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Reason = command.Reason.Trim(),
            Status = RequestStatus.Draft
        };

        db.AbsenceRequests.Add(request);
        await db.SaveChangesAsync(ct);

        return Results.Created((string?)null, new { id = request.Id });
    }
}
