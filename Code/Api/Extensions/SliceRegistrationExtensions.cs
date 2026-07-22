using VacaFlow.Api.Slices.Auth;
using VacaFlow.Api.Slices.Security;

namespace VacaFlow.Api.Extensions;

/// <summary>Wires every vertical slice's endpoints into the Minimal API pipeline.</summary>
public static class SliceRegistrationExtensions
{
    public static void RegisterAllSlices(this IEndpointRouteBuilder app)
    {
        AntiforgeryTokenSlice.Register(app);
        RegisterSlice.Register(app);
    }
}
