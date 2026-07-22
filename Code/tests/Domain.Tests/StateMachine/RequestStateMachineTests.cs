using VacaFlow.Domain.Entities;
using VacaFlow.Domain.Enums;
using VacaFlow.Domain.Exceptions;
using VacaFlow.Domain.StateMachine;

namespace Domain.Tests.StateMachine;

public class RequestStateMachineTests
{
    private static AbsenceRequest RequestWith(RequestStatus status) => new()
    {
        Id = Guid.NewGuid(),
        OwnerEmployeeId = Guid.NewGuid(),
        AbsenceTypeId = Guid.NewGuid(),
        StartDate = new DateOnly(2026, 8, 1),
        EndDate = new DateOnly(2026, 8, 2),
        Reason = "x",
        Status = status
    };

    [Theory]
    [InlineData(RequestStatus.Draft, RequestAction.Submit, RequestStatus.Submitted)]
    [InlineData(RequestStatus.Draft, RequestAction.Cancel, RequestStatus.Cancelled)]
    [InlineData(RequestStatus.Submitted, RequestAction.Approve, RequestStatus.Approved)]
    [InlineData(RequestStatus.Submitted, RequestAction.Reject, RequestStatus.Rejected)]
    [InlineData(RequestStatus.Submitted, RequestAction.Cancel, RequestStatus.Cancelled)]
    public void Valid_transitions_change_status(RequestStatus from, RequestAction action, RequestStatus to)
    {
        var request = RequestWith(from);
        RequestStateMachine.Transition(request, action);
        Assert.Equal(to, request.Status);
    }

    [Theory]
    [InlineData(RequestStatus.Draft, RequestAction.Approve)]
    [InlineData(RequestStatus.Draft, RequestAction.Reject)]
    [InlineData(RequestStatus.Submitted, RequestAction.Submit)]
    [InlineData(RequestStatus.Approved, RequestAction.Cancel)]
    [InlineData(RequestStatus.Approved, RequestAction.Approve)]
    [InlineData(RequestStatus.Rejected, RequestAction.Cancel)]
    [InlineData(RequestStatus.Cancelled, RequestAction.Submit)]
    [InlineData(RequestStatus.Cancelled, RequestAction.Cancel)]
    public void Invalid_transitions_throw_and_do_not_change_status(RequestStatus from, RequestAction action)
    {
        var request = RequestWith(from);
        Assert.Throws<DomainException>(() => RequestStateMachine.Transition(request, action));
        Assert.Equal(from, request.Status);
    }

    [Fact]
    public void Terminal_states_have_no_valid_actions()
    {
        Assert.Empty(RequestStateMachine.ValidActions(RequestStatus.Approved));
        Assert.Empty(RequestStateMachine.ValidActions(RequestStatus.Rejected));
        Assert.Empty(RequestStateMachine.ValidActions(RequestStatus.Cancelled));
    }
}
