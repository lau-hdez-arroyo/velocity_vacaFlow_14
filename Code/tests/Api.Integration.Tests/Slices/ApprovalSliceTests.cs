using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VacaFlow.Domain.Enums;
using VacaFlow.Infrastructure.Persistence;

namespace Api.Integration.Tests.Slices;

/// <summary>US-007 — Manager approves/rejects a Submitted request (AC-001..008).</summary>
public class ApprovalSliceTests : IClassFixture<VacaFlowWebApplicationFactory>
{
    private readonly VacaFlowWebApplicationFactory _factory;

    public ApprovalSliceTests(VacaFlowWebApplicationFactory factory) => _factory = factory;

    private static DateOnly Future(int days) => DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(days);

    private async Task<HttpClient> SeededEmployeeAsync()
        => await _factory.LoginAsync(SeedDataInitializer.SeedEmployeeEmail, SeedDataInitializer.SeedEmployeePassword);

    private async Task<HttpClient> SeededManagerAsync()
        => await _factory.LoginAsync(SeedDataInitializer.SeedManagerEmail, SeedDataInitializer.SeedManagerPassword);

    private async Task<Guid> SubmittedRequestAsync(HttpClient employee)
    {
        var typeId = await employee.FirstAbsenceTypeIdAsync();
        var id = await employee.CreateRequestAsync(typeId, Future(3), Future(5), "Please review");
        (await employee.PostAsync($"/api/requests/{id}/submit", null)).EnsureSuccessStatusCode();
        return id;
    }

    [Fact] // AC-002 — approve creates exactly one Approval and transitions state
    public async Task Approve_transitions_to_approved_and_records_one_decision()
    {
        var employee = await SeededEmployeeAsync();
        var id = await SubmittedRequestAsync(employee);
        var manager = await SeededManagerAsync();

        var response = await manager.PostAsJsonAsync($"/api/requests/{id}/approve", new { comment = "Approved — enjoy!" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VacaFlowDbContext>();
        Assert.Equal(RequestStatus.Approved, (await db.AbsenceRequests.SingleAsync(r => r.Id == id)).Status);
        Assert.Equal(1, await db.Approvals.CountAsync(a => a.RequestId == id));
        var approval = await db.Approvals.SingleAsync(a => a.RequestId == id);
        Assert.Equal(ApprovalDecision.Approved, approval.Decision);
        Assert.Equal("Approved — enjoy!", approval.Comment);
    }

    [Fact] // US-008 AC-002 — employee sees the decision + comment in their list
    public async Task Employee_list_shows_decision_and_comment_after_approval()
    {
        var employee = await SeededEmployeeAsync();
        var id = await SubmittedRequestAsync(employee);
        var manager = await SeededManagerAsync();
        (await manager.PostAsJsonAsync($"/api/requests/{id}/approve", new { comment = "See you when you're back" }))
            .EnsureSuccessStatusCode();

        var list = await employee.GetFromJsonAsync<List<RequestListRow>>("/api/requests");
        var row = Assert.Single(list!, r => r.Id == id);
        Assert.Equal("Approved", row.Status);
        Assert.Equal("Approved", row.Decision);
        Assert.Equal("See you when you're back", row.DecisionComment);
    }

    private record RequestListRow(Guid Id, string Status, string? Decision, string? DecisionComment);

    [Fact] // AC-003 — reject
    public async Task Reject_transitions_to_rejected()
    {
        var employee = await SeededEmployeeAsync();
        var id = await SubmittedRequestAsync(employee);
        var manager = await SeededManagerAsync();

        var response = await manager.PostAsJsonAsync($"/api/requests/{id}/reject", new { comment = "Need more coverage." });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VacaFlowDbContext>();
        Assert.Equal(RequestStatus.Rejected, (await db.AbsenceRequests.SingleAsync(r => r.Id == id)).Status);
    }

    [Fact] // AC-005 — cannot approve a non-Submitted request
    public async Task Approve_draft_returns_422()
    {
        var employee = await SeededEmployeeAsync();
        var typeId = await employee.FirstAbsenceTypeIdAsync();
        var id = await employee.CreateRequestAsync(typeId, Future(3), Future(5), "Still a draft");
        var manager = await SeededManagerAsync();

        var response = await manager.PostAsJsonAsync($"/api/requests/{id}/approve", new { comment = (string?)null });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact] // AC-006 — a manager who is not the assignee cannot decide
    public async Task Approve_by_unassigned_manager_returns_403()
    {
        var employee = await SeededEmployeeAsync();
        var id = await SubmittedRequestAsync(employee);
        var otherManager = await _factory.RegisterAsync("Other Mgr", "othermgr@example.com", "Passw0rd!", Role.Manager);

        var response = await otherManager.PostAsJsonAsync($"/api/requests/{id}/approve", new { comment = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // BR-MGR-002 — an employee cannot approve
    public async Task Approve_by_employee_returns_403()
    {
        var employee = await SeededEmployeeAsync();
        var id = await SubmittedRequestAsync(employee);

        var response = await employee.PostAsJsonAsync($"/api/requests/{id}/approve", new { comment = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // AC-008 — a second decision is rejected
    public async Task Second_decision_returns_409()
    {
        var employee = await SeededEmployeeAsync();
        var id = await SubmittedRequestAsync(employee);
        var manager = await SeededManagerAsync();

        (await manager.PostAsJsonAsync($"/api/requests/{id}/approve", new { comment = (string?)null })).EnsureSuccessStatusCode();
        var again = await manager.PostAsJsonAsync($"/api/requests/{id}/reject", new { comment = (string?)null });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact] // AC-007 — a manager cannot decide on their own request (assigned to self)
    public async Task Manager_cannot_approve_their_own_request()
    {
        var manager = await _factory.RegisterAsync("Self Mgr", "selfmgr@example.com", "Passw0rd!", Role.Manager);

        // Assign the manager to themselves so the assignment check passes and the self-approval guard is exercised.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VacaFlowDbContext>();
            var self = await db.Employees.SingleAsync(e => e.Email == "selfmgr@example.com");
            self.ManagerId = self.Id;
            await db.SaveChangesAsync();
        }

        var id = await SubmittedRequestAsync(manager);
        var response = await manager.PostAsJsonAsync($"/api/requests/{id}/approve", new { comment = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
