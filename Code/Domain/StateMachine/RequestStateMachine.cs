using VacaFlow.Domain.Entities;
using VacaFlow.Domain.Enums;
using VacaFlow.Domain.Exceptions;

namespace VacaFlow.Domain.StateMachine;

/// <summary>
/// The single authority for request lifecycle transitions. Slice handlers call
/// <see cref="Transition"/> before persisting; an invalid (status, action) pair raises
/// <see cref="DomainException"/> (mapped to HTTP 422). Terminal states have no transitions
/// (BR-STATE-001). Cancel is valid from Draft and Submitted (BR-REQ-005).
/// </summary>
public static class RequestStateMachine
{
    private static readonly Dictionary<(RequestStatus, RequestAction), RequestStatus> Transitions = new()
    {
        [(RequestStatus.Draft, RequestAction.Submit)] = RequestStatus.Submitted,
        [(RequestStatus.Draft, RequestAction.Cancel)] = RequestStatus.Cancelled,
        [(RequestStatus.Submitted, RequestAction.Approve)] = RequestStatus.Approved,
        [(RequestStatus.Submitted, RequestAction.Reject)] = RequestStatus.Rejected,
        [(RequestStatus.Submitted, RequestAction.Cancel)] = RequestStatus.Cancelled,
    };

    /// <summary>Applies <paramref name="action"/> to <paramref name="request"/>, mutating its status. Throws on invalid transitions.</summary>
    public static void Transition(AbsenceRequest request, RequestAction action)
    {
        if (!Transitions.TryGetValue((request.Status, action), out var next))
            throw new DomainException($"Cannot apply '{action}' to a request in '{request.Status}' state.");

        request.Status = next;
    }

    /// <summary>Valid actions available from a given status (used for UI action hints).</summary>
    public static IReadOnlyList<RequestAction> ValidActions(RequestStatus status)
        => Transitions.Keys.Where(k => k.Item1 == status).Select(k => k.Item2).ToList();
}
