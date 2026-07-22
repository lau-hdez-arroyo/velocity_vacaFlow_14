using System.Net.Http.Json;
using VacaFlow.Domain.Enums;

namespace Api.Integration.Tests;

/// <summary>Helpers to obtain authenticated clients (cookies persist per HttpClient).</summary>
internal static class TestApi
{
    public static async Task<HttpClient> RegisterAsync(
        this VacaFlowWebApplicationFactory factory, string fullName, string email, string password, Role role)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName, email, password, role = role.ToString() });
        response.EnsureSuccessStatusCode();
        return client;
    }

    public static async Task<HttpClient> LoginAsync(
        this VacaFlowWebApplicationFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return client;
    }

    public static async Task<Guid> CreateRequestAsync(
        this HttpClient client, Guid absenceTypeId, DateOnly start, DateOnly end, string reason)
    {
        var response = await client.PostAsJsonAsync("/api/requests",
            new { absenceTypeId, startDate = start, endDate = end, reason });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        return created!.Id;
    }

    public static async Task<Guid> FirstAbsenceTypeIdAsync(this HttpClient client)
    {
        var types = await client.GetFromJsonAsync<List<AbsenceTypeResponse>>("/api/absence-types");
        return types!.First().Id;
    }

    public record CreatedResponse(Guid Id);
    public record AbsenceTypeResponse(Guid Id, string Name);
}
