using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using VacaFlow.Domain.Enums;
using VacaFlow.Infrastructure.Persistence;
using static VacaFlow.Api.Slices.Auth.RegisterSlice;

namespace Api.Integration.Tests.Slices;

/// <summary>End-to-end tests for POST /api/auth/register (AC-001..AC-005).</summary>
public class AuthSliceTests : IClassFixture<VacaFlowWebApplicationFactory>
{
    private readonly VacaFlowWebApplicationFactory _factory;

    public AuthSliceTests(VacaFlowWebApplicationFactory factory) => _factory = factory;

    [Fact] // AC-001
    public async Task Register_valid_employee_creates_account_and_establishes_session()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterCommand("Alice Anderson", "alice@example.com", "Passw0rd!", Role.Employee));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.Contains(".AspNetCore.Cookies"));

        var body = await response.Content.ReadAsStringAsync();
        // AC-004 — no plaintext password or hash in the response
        Assert.DoesNotContain("Passw0rd!", body);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VacaFlowDbContext>();
        var employee = db.Employees.Single(e => e.Email == "alice@example.com");
        Assert.Equal(Role.Employee, employee.Role);
        Assert.Null(employee.ManagerId);
        // AC-004 — stored value is a BCrypt hash, not the plaintext
        Assert.NotEqual("Passw0rd!", employee.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Passw0rd!", employee.PasswordHash));
    }

    [Fact] // AC-002
    public async Task Register_valid_manager_persists_manager_role()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterCommand("Mary Manager", "mary@example.com", "Passw0rd!", Role.Manager));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<RegisteredUserDto>();
        Assert.Equal("Manager", dto!.Role);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VacaFlowDbContext>();
        Assert.Equal(Role.Manager, db.Employees.Single(e => e.Email == "mary@example.com").Role);
    }

    [Fact] // AC-003
    public async Task Register_duplicate_email_returns_409_and_creates_no_second_row()
    {
        var client = _factory.CreateClient();
        var first = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterCommand("Bob Base", "bob@example.com", "Passw0rd!", Role.Employee));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterCommand("Bob Clone", "bob@example.com", "Different1!", Role.Employee));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VacaFlowDbContext>();
        Assert.Equal(1, db.Employees.Count(e => e.Email == "bob@example.com"));
    }

    [Fact] // AC-003 — email match is case-insensitive
    public async Task Register_duplicate_email_differing_case_returns_409()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterCommand("Case One", "case@example.com", "Passw0rd!", Role.Employee));

        var second = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterCommand("Case Two", "CASE@EXAMPLE.COM", "Passw0rd!", Role.Employee));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact] // AC-002 — role sent as a JSON string, as the frontend does
    public async Task Register_accepts_role_as_json_string()
    {
        var client = _factory.CreateClient();
        var json = """{"fullName":"Sam String","email":"sam@example.com","password":"Passw0rd!","role":"Manager"}""";

        var response = await client.PostAsync("/api/auth/register",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<RegisteredUserDto>();
        Assert.Equal("Manager", dto!.Role);
    }

    [Fact] // AC-005
    public async Task Register_missing_fields_returns_400_with_field_errors_and_no_row()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterCommand("", "", "", Role.Employee));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", body);
        Assert.Contains("fullName", body);
        Assert.Contains("email", body);
        Assert.Contains("password", body);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VacaFlowDbContext>();
        Assert.False(db.Employees.Any(e => e.FullName == ""));
    }
}
