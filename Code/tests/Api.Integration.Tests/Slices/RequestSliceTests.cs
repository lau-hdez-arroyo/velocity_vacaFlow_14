using System.Net;
using System.Net.Http.Json;
using VacaFlow.Domain.Enums;

namespace Api.Integration.Tests.Slices;

/// <summary>US-003…US-006 — request lifecycle (create / edit / submit / cancel / list).</summary>
public class RequestSliceTests : IClassFixture<VacaFlowWebApplicationFactory>
{
    private readonly VacaFlowWebApplicationFactory _factory;

    public RequestSliceTests(VacaFlowWebApplicationFactory factory) => _factory = factory;

    private static DateOnly Future(int days) => DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(days);

    private async Task<(HttpClient client, Guid typeId)> EmployeeAsync(string email)
    {
        var client = await _factory.RegisterAsync("Emp " + email, email, "Passw0rd!", Role.Employee);
        return (client, await client.FirstAbsenceTypeIdAsync());
    }

    [Fact] // US-003 AC-001 + AC-006 list
    public async Task Create_draft_persists_and_appears_in_own_list()
    {
        var (client, typeId) = await EmployeeAsync("create1@example.com");
        var id = await client.CreateRequestAsync(typeId, Future(3), Future(5), "Family trip");

        var list = await client.GetFromJsonAsync<List<RequestRow>>("/api/requests");
        Assert.Contains(list!, r => r.Id == id && r.Status == "Draft");
    }

    [Fact] // US-003 AC-003 — start in the past
    public async Task Create_with_past_start_returns_400()
    {
        var (client, typeId) = await EmployeeAsync("create2@example.com");
        var response = await client.PostAsJsonAsync("/api/requests",
            new { absenceTypeId = typeId, startDate = Future(-2), endDate = Future(2), reason = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] // US-003 AC-004 — end before start
    public async Task Create_with_end_before_start_returns_400()
    {
        var (client, typeId) = await EmployeeAsync("create3@example.com");
        var response = await client.PostAsJsonAsync("/api/requests",
            new { absenceTypeId = typeId, startDate = Future(5), endDate = Future(3), reason = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] // US-003 AC-006 — missing reason
    public async Task Create_with_missing_reason_returns_400()
    {
        var (client, typeId) = await EmployeeAsync("create4@example.com");
        var response = await client.PostAsJsonAsync("/api/requests",
            new { absenceTypeId = typeId, startDate = Future(3), endDate = Future(4), reason = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] // US-002 AC-006 — auth required
    public async Task Create_unauthenticated_returns_401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/requests",
            new { absenceTypeId = Guid.NewGuid(), startDate = Future(3), endDate = Future(4), reason = "x" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact] // US-004 AC-001
    public async Task Edit_draft_persists_changes()
    {
        var (client, typeId) = await EmployeeAsync("edit1@example.com");
        var id = await client.CreateRequestAsync(typeId, Future(3), Future(5), "Original");

        var edit = await client.PutAsJsonAsync($"/api/requests/{id}",
            new { absenceTypeId = typeId, startDate = Future(6), endDate = Future(8), reason = "Updated" });
        Assert.Equal(HttpStatusCode.NoContent, edit.StatusCode);

        var detail = await client.GetFromJsonAsync<RequestRow>($"/api/requests/{id}");
        Assert.Equal("Updated", detail!.Reason);
    }

    [Fact] // US-004 AC-004 — non-owner cannot edit
    public async Task Edit_by_non_owner_returns_403()
    {
        var (owner, typeId) = await EmployeeAsync("edit-owner@example.com");
        var id = await owner.CreateRequestAsync(typeId, Future(3), Future(5), "Mine");

        var (other, otherType) = await EmployeeAsync("edit-other@example.com");
        var response = await other.PutAsJsonAsync($"/api/requests/{id}",
            new { absenceTypeId = otherType, startDate = Future(3), endDate = Future(5), reason = "Hijack" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // US-004 AC-003 — non-Draft cannot be edited
    public async Task Edit_submitted_request_returns_422()
    {
        var (client, typeId) = await EmployeeAsync("edit-sub@example.com");
        var id = await client.CreateRequestAsync(typeId, Future(3), Future(5), "To submit");
        (await client.PostAsync($"/api/requests/{id}/submit", null)).EnsureSuccessStatusCode();

        var response = await client.PutAsJsonAsync($"/api/requests/{id}",
            new { absenceTypeId = typeId, startDate = Future(3), endDate = Future(5), reason = "late edit" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact] // US-005 AC-001
    public async Task Submit_transitions_draft_to_submitted()
    {
        var (client, typeId) = await EmployeeAsync("submit1@example.com");
        var id = await client.CreateRequestAsync(typeId, Future(3), Future(5), "Ready");

        var submit = await client.PostAsync($"/api/requests/{id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submit.StatusCode);

        var detail = await client.GetFromJsonAsync<RequestRow>($"/api/requests/{id}");
        Assert.Equal("Submitted", detail!.Status);
    }

    [Fact] // US-005 AC-003 — non-owner cannot submit
    public async Task Submit_by_non_owner_returns_403()
    {
        var (owner, typeId) = await EmployeeAsync("submit-owner@example.com");
        var id = await owner.CreateRequestAsync(typeId, Future(3), Future(5), "Mine");

        var (other, _) = await EmployeeAsync("submit-other@example.com");
        var response = await other.PostAsync($"/api/requests/{id}/submit", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // US-006 AC-001 — cancel from Draft
    public async Task Cancel_draft_transitions_to_cancelled()
    {
        var (client, typeId) = await EmployeeAsync("cancel1@example.com");
        var id = await client.CreateRequestAsync(typeId, Future(3), Future(5), "Nope");
        (await client.PostAsync($"/api/requests/{id}/cancel", null)).EnsureSuccessStatusCode();

        var detail = await client.GetFromJsonAsync<RequestRow>($"/api/requests/{id}");
        Assert.Equal("Cancelled", detail!.Status);
    }

    [Fact] // US-006 — cancel from Submitted (BR-REQ-005 / state machine)
    public async Task Cancel_submitted_transitions_to_cancelled()
    {
        var (client, typeId) = await EmployeeAsync("cancel2@example.com");
        var id = await client.CreateRequestAsync(typeId, Future(3), Future(5), "Later nope");
        (await client.PostAsync($"/api/requests/{id}/submit", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/requests/{id}/cancel", null)).EnsureSuccessStatusCode();

        var detail = await client.GetFromJsonAsync<RequestRow>($"/api/requests/{id}");
        Assert.Equal("Cancelled", detail!.Status);
    }

    [Fact] // US-006 AC-002 — cancel of a terminal state is rejected
    public async Task Cancel_terminal_request_returns_422()
    {
        var (client, typeId) = await EmployeeAsync("cancel3@example.com");
        var id = await client.CreateRequestAsync(typeId, Future(3), Future(5), "Nope");
        (await client.PostAsync($"/api/requests/{id}/cancel", null)).EnsureSuccessStatusCode();

        var again = await client.PostAsync($"/api/requests/{id}/cancel", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
    }

    private record RequestRow(Guid Id, string EmployeeName, string AbsenceType, string Reason, string Status, string? Decision, string? DecisionComment);
}
