# VacaFlow_14

Local-execution absence/vacation request manager for IGS Solutions. .NET 8 Minimal API
(Vertical Slice Onion) + EF Core 8 / SQLite, with a Next.js 14 frontend.

> **Note on runtime:** the projects target **.NET 8** but this workstation only has the
> ASP.NET Core **10** shared runtime installed, so `Code/Directory.Build.props` sets
> `<RollForward>Major</RollForward>` to let the net8.0 assemblies run on it. Installing the
> ASP.NET Core 8.0.x runtime and removing that setting is equally valid.

## Prerequisites

- .NET SDK 8+ (10 works via roll-forward), Node.js 18/20+, npm.

## Run (two terminals)

```bash
# Terminal 1 — API (http://localhost:5000)
cd Code/Api
dotnet run
```

On first run the API auto-applies EF Core migrations (creates `vacaflow.db`) and seeds the
absence-type catalog + a manager and an assigned employee — no manual migrate/seed step.

```bash
# Terminal 2 — Frontend (http://localhost:3000)
cd Code/Web
npm install
npm run dev
```

Open <http://localhost:3000/register> to create an account.

## Seeded accounts (documented, non-real — BR-SEC-002)

| Role | Email | Password |
|------|-------|----------|
| Manager | `manager@vacaflow.local` | `Manager#12345` |
| Employee (assigned to the manager) | `employee@vacaflow.local` | `Employee#12345` |

Passwords are stored only as BCrypt hashes. The seed is idempotent.

## Reset the database

Stop the API, delete `Code/Api/vacaflow.db`, and restart — migrations and seeding re-run.
The `.db` file is git-ignored and must never be committed.

## Tests

```bash
cd Code
dotnet test
```
