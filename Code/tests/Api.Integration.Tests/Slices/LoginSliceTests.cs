using System.Net;
using System.Net.Http.Json;
using VacaFlow.Domain.Enums;
using VacaFlow.Infrastructure.Persistence;

namespace Api.Integration.Tests.Slices;

/// <summary>US-002 — Login / Logout / Current user (AC-001..006).</summary>
public class LoginSliceTests : IClassFixture<VacaFlowWebApplicationFactory>
{
    private readonly VacaFlowWebApplicationFactory _factory;

    public LoginSliceTests(VacaFlowWebApplicationFactory factory) => _factory = factory;

    [Fact] // AC-001 + AC-004
    public async Task Login_with_valid_credentials_establishes_session()
    {
        await _factory.RegisterAsync("Log In", "login@example.com", "Passw0rd!", Role.Employee);
        var client = await _factory.LoginAsync("login@example.com", "Passw0rd!");

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var body = await me.Content.ReadAsStringAsync();
        Assert.Contains("login@example.com", body);
        Assert.Contains("Employee", body);
    }

    [Fact] // AC-002 — wrong password, generic 401
    public async Task Login_with_wrong_password_returns_generic_401()
    {
        await _factory.RegisterAsync("Wrong Pw", "wrongpw@example.com", "Passw0rd!", Role.Employee);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "wrongpw@example.com", password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password was", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] // AC-002 — unknown email, generic 401 (no disclosure)
    public async Task Login_with_unknown_email_returns_401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@example.com", password = "Passw0rd!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact] // AC-003 — logout invalidates the session
    public async Task Logout_invalidates_session()
    {
        var client = await _factory.RegisterAsync("Bye Bye", "logout@example.com", "Passw0rd!", Role.Employee);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact] // AC-005 / AC-006 — unauthenticated access is refused
    public async Task Unauthenticated_requests_return_401()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/requests")).StatusCode);
    }

    [Fact] // login routes are seeded and usable
    public async Task Seeded_manager_can_log_in()
    {
        var client = await _factory.LoginAsync(SeedDataInitializer.SeedManagerEmail, SeedDataInitializer.SeedManagerPassword);
        var body = await (await client.GetAsync("/api/auth/me")).Content.ReadAsStringAsync();
        Assert.Contains("Manager", body);
    }
}
