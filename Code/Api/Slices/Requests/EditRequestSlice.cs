using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacaFlow.Api.Extensions;
using VacaFlow.Application.Exceptions;
using VacaFlow.Application.Validation;
using VacaFlow.Domain.Enums;
using VacaFlow.Domain.Exceptions;
using VacaFlow.Infrastructure.Persistence;

namespace VacaFlow.Api.Slices.Requests;

/// <summary>
/// US-004 — Edit a Draft request. Only the owner (BR-REQ-004) and only while Draft (BR-REQ-003);
/// dates/reason are re-validated (BR-REQ-001/002, BR-FIELD-001).
/// </summary>
public static class EditRequestSlice
{
    public record EditRequestCommand(Guid AbsenceTypeId, DateOnly StartDate, DateOnly EndDate, string Reason);

    public class EditRequestValidator : IValidator<EditRequestCommand>
    {
        public ValidationResult Validate(EditRequestCommand c)
        {
            var errors = RequestFieldRules.Check(c.AbsenceTypeId, c.StartDate, c.EndDate, c.Reason);
            return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
        }
    }

    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/requests/{id:guid}", HandleAsync).RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromBody] EditRequestCommand command,
        HttpContext httpContext,
        VacaFlowDbContext db,
        ValidationPipelineInvoker validation,
        CancellationToken ct)
    {
        validation.Validate(command);
        var meId = httpContext.User.EmployeeId();

        var request = await db.AbsenceRequests.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Request {id} not found.");

        if (request.OwnerEmployeeId != meId)
            throw new AuthorizationException("You can only edit your own requests.");
        if (request.Status != RequestStatus.Draft)
            throw new DomainException("Only draft requests can be edited.");

        if (!await db.AbsenceTypes.AnyAsync(a => a.Id == command.AbsenceTypeId, ct))
            throw new NotFoundException("The selected absence type does not exist.");

        request.AbsenceTypeId = command.AbsenceTypeId;
        request.StartDate = command.StartDate;
        request.EndDate = command.EndDate;
        request.Reason = command.Reason.Trim();

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
