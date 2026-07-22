using Microsoft.EntityFrameworkCore;
using VacaFlow.Domain.Entities;
using VacaFlow.Domain.Enums;
using VacaFlow.Domain.Ports;

namespace VacaFlow.Infrastructure.Persistence;

/// <summary>
/// Idempotent startup seed. Runs after migration and never duplicates existing rows.
/// Covers US-SH-001 (absence-type catalog) and US-SH-002 (a manager account + an
/// assigned employee). Seed credentials are documented, non-real (BR-SEC-002) — see Code/README.md.
/// </summary>
public static class SeedDataInitializer
{
    public const string SeedManagerEmail = "manager@vacaflow.local";
    public const string SeedManagerPassword = "Manager#12345";
    public const string SeedEmployeeEmail = "employee@vacaflow.local";
    public const string SeedEmployeePassword = "Employee#12345";

    private static readonly string[] AbsenceTypeNames = { "Vacation", "Personal Leave", "Sick Leave" };

    public static async Task SeedAsync(VacaFlowDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        // US-SH-001 — absence-type catalog (idempotent by name)
        foreach (var name in AbsenceTypeNames)
        {
            if (!await db.AbsenceTypes.AnyAsync(a => a.Name == name, ct))
                db.AbsenceTypes.Add(new AbsenceType { Id = Guid.NewGuid(), Name = name });
        }

        // US-SH-002 — one manager account (idempotent by email)
        var manager = await db.Employees.FirstOrDefaultAsync(e => e.Email == SeedManagerEmail, ct);
        if (manager is null)
        {
            manager = new Employee
            {
                Id = Guid.NewGuid(),
                FullName = "Seed Manager",
                Email = SeedManagerEmail,
                PasswordHash = hasher.Hash(SeedManagerPassword),
                Role = Role.Manager,
                ManagerId = null
            };
            db.Employees.Add(manager);
        }

        // US-SH-002 — one employee assigned to that manager (idempotent by email)
        if (!await db.Employees.AnyAsync(e => e.Email == SeedEmployeeEmail, ct))
        {
            db.Employees.Add(new Employee
            {
                Id = Guid.NewGuid(),
                FullName = "Seed Employee",
                Email = SeedEmployeeEmail,
                PasswordHash = hasher.Hash(SeedEmployeePassword),
                Role = Role.Employee,
                ManagerId = manager.Id
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
