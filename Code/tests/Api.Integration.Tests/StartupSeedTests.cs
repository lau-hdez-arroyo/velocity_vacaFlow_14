using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VacaFlow.Domain.Enums;
using VacaFlow.Domain.Ports;
using VacaFlow.Infrastructure.Persistence;

namespace Api.Integration.Tests;

/// <summary>Startup auto-migration + seed (AC-006, US-SH-001, US-SH-002).</summary>
public class StartupSeedTests : IClassFixture<VacaFlowWebApplicationFactory>
{
    private readonly VacaFlowWebApplicationFactory _factory;

    public StartupSeedTests(VacaFlowWebApplicationFactory factory) => _factory = factory;

    [Fact] // AC-006 + US-SH-001 + US-SH-002
    public async Task Startup_migrates_schema_and_seeds_catalog_manager_and_assigned_employee()
    {
        _ = _factory.CreateClient(); // forces host start -> Program.cs migrate + seed

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VacaFlowDbContext>();

        Assert.Equal(3, await db.AbsenceTypes.CountAsync());
        Assert.True(await db.Employees.AnyAsync(e => e.Role == Role.Manager));

        var seededEmployee = await db.Employees
            .SingleAsync(e => e.Email == SeedDataInitializer.SeedEmployeeEmail);
        var seededManager = await db.Employees
            .SingleAsync(e => e.Email == SeedDataInitializer.SeedManagerEmail);
        Assert.Equal(seededManager.Id, seededEmployee.ManagerId);
    }

    [Fact] // US-SH-001 AC-002 / US-SH-002 AC-004 — idempotency
    public async Task Reseeding_creates_no_duplicates()
    {
        _ = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VacaFlowDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await SeedDataInitializer.SeedAsync(db, hasher);
        await SeedDataInitializer.SeedAsync(db, hasher);

        Assert.Equal(3, await db.AbsenceTypes.CountAsync());
        Assert.Equal(1, await db.Employees.CountAsync(e => e.Email == SeedDataInitializer.SeedManagerEmail));
        Assert.Equal(1, await db.Employees.CountAsync(e => e.Email == SeedDataInitializer.SeedEmployeeEmail));
    }
}
