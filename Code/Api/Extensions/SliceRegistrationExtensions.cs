using VacaFlow.Api.Slices.Approvals;
using VacaFlow.Api.Slices.Auth;
using VacaFlow.Api.Slices.Requests;
using VacaFlow.Api.Slices.Security;

namespace VacaFlow.Api.Extensions;

/// <summary>Wires every vertical slice's endpoints into the Minimal API pipeline.</summary>
public static class SliceRegistrationExtensions
{
    public static void RegisterAllSlices(this IEndpointRouteBuilder app)
    {
        // Security
        AntiforgeryTokenSlice.Register(app);

        // Auth (US-001, US-002)
        RegisterSlice.Register(app);
        LoginSlice.Register(app);
        LogoutSlice.Register(app);
        CurrentUserSlice.Register(app);

        // Absence requests (US-003…US-006, US-008)
        GetAbsenceTypesSlice.Register(app);
        CreateRequestSlice.Register(app);
        GetRequestsSlice.Register(app);
        GetRequestDetailSlice.Register(app);
        EditRequestSlice.Register(app);
        SubmitRequestSlice.Register(app);
        CancelRequestSlice.Register(app);

        // Approvals (US-007)
        ApproveRequestSlice.Register(app);
        RejectRequestSlice.Register(app);
    }
}
