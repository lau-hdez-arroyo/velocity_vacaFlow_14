# US-002 — Manage Employees: Implementation Plan

**Story:** US-002 (Docs/backlog.md, Sprint 1, Must Have) · **Source FRs:** FR-EM-001…FR-EM-010 · **Business rules:** BR-022/BR-EMP-001, BR-017/BR-EMP-004/005, BR-003/BR-EMP-002, BR-023, BR-EMP-003, BR-AUD-003, BR-SEC-004
**Repo state:** greenfield — this plan begins with the full solution bootstrap (Stage A) and then implements US-002 (Stages B–D).
**Status:** Approved by Edison Suarez on 2026-07-17.

## Context

Evalux replaces a spreadsheet-and-email evaluation process. US-002 delivers the employee master data foundation: without accurate, active-flagged employee records, no evaluation (US-006+) can be created. Since no code exists yet, this plan also bootstraps the entire solution skeleton per `Docs/tech-doc.md` §4–§5 and `Docs/code-standards.md`, adjusted by the decisions confirmed with the user (recorded in §6).

---

## 1. Story understanding (what, why, and what for)

**What:** HR Coordinators can create, view, update, activate, and deactivate employee records, and search them by partial name or identification number. An employee has: identification number (unique, absolute — BR-EMP-001), full name, email, job title, department, and an active status. New records are created Active with a system-generated ID. Deactivation is a soft state change — never physical deletion (BR-017/BR-EMP-004/005) — and deactivated employees stay visible, searchable, and keep their historical evaluations intact (BR-EMP-003). Only active employees may receive new evaluations (BR-003/BR-EMP-002 — enforced at evaluation creation, US-006; US-002 must expose the status for that check). Every create/update/activate/deactivate is an audited critical action (BR-AUD-003) recorded atomically with the business change (single-commit staging pattern).

**Why:** the current spreadsheet process has no reliable employee registry — duplicates, stale records, and no traceability of who changed what.

**What for:** a trustworthy roster that is the basis for evaluations: uniqueness guarantees identity integrity, soft-deactivation preserves history for the 7-year retention requirement, and the audit trail makes every change accountable.

**Acceptance criteria (verbatim backbone, from Docs/backlog.md):**
- AC-001 Successful employee creation (all required fields → Active record, system ID, success confirmation showing the new employee ID)
- AC-002 Duplicate identification number rejected (active or inactive; error "identification number already in use"; no record created)
- AC-003 Update does not retroactively affect finalized evaluations
- AC-004 Deactivation retains employee and historical evaluations (visible, searchable, history unchanged)
- AC-005 Inactive employee cannot receive a new evaluation (enforcement point lives in evaluation creation — see UC-H)
- AC-006 Search returns active and inactive employees (partial name or identification number)
- AC-007 Missing required field on creation → field-level validation error, no record created

---

## 2. Full-stack impact analysis

| Tier | Verdict | Work |
|---|---|---|
| **Database** | **Yes** | New `Employees` table (unique index on identification number) and new `AuditLogs` table (minimal audit, per user decision); one EF migration. |
| **Backend** | **Yes** | Bootstrap the whole solution (6 src + 4 tests projects); then `Evalux.Modules.Employees` (entity, repository, validators, service, contracts), minimal `Evalux.Modules.Audit` (`AuditLog` + staging `AuditService`), shared `EvaluxDbContext`/`UnitOfWork` in `Evalux.Api`, and 6 REST endpoints under `/api/v1/employees`. No auth in this story (user decision — retrofitted by US-001). |
| **Frontend** | **Yes** | Bootstrap Next.js app under `Code/frontend/`; app shell with sidebar nav (per `Docs/UI mockups/screenshot 1–2.png`); Employees page: search, table with status badges, create/edit modals, activate/deactivate with confirmation, field-level error display, success confirmation with new employee ID. |

---

## 3. Database changes

One migration: `Employees_InitialSchema` (in `Code/src/Evalux.Api/Infrastructure/Migrations/`). Both tables are new — purely additive, safe.

### Table `dbo.Employees` (owned by Modules.Employees, config `EmployeeEntityConfiguration`)

| Column | SQL type | Null | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | NOT NULL | PK (clustered). Client-generated `Guid.CreateVersion7()` in the entity factory (sequential, index-friendly). |
| `IdentificationNumber` | `nvarchar(50)` | NOT NULL | **Unique index `UX_Employees_IdentificationNumber`** — DB backstop for BR-022/BR-EMP-001 (applies across active + inactive). |
| `FullName` | `nvarchar(200)` | NOT NULL | Nonclustered index `IX_Employees_FullName` (prefix search help; contains-search scans are acceptable at this scale per NFR-PERF-001). |
| `Email` | `nvarchar(254)` | NOT NULL | Format validated in FluentValidation; no DB constraint. |
| `JobTitle` | `nvarchar(100)` | NOT NULL | |
| `Department` | `nvarchar(100)` | NOT NULL | |
| `IsActive` | `bit` | NOT NULL | Default `1`. Never deleted — deactivation flips this flag (BR-017). |
| `CreatedAtUtc` | `datetimeoffset` | NOT NULL | `DateTimeOffset.UtcNow` (BR-023; code-standards §4.4 wins over tech-doc's `DateTime`). |
| `UpdatedAtUtc` | `datetimeoffset` | NULL | Set on update/activate/deactivate. |

### Table `dbo.AuditLogs` (owned by Modules.Audit, config `AuditLogEntityConfiguration`)

| Column | SQL type | Null | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | NOT NULL | PK. `Guid.CreateVersion7()`. |
| `ActorId` | `nvarchar(100)` | NOT NULL | Placeholder actor until US-001 (see §6, `IActorProvider`). |
| `OccurredAtUtc` | `datetimeoffset` | NOT NULL | UTC (BR-023). |
| `EntityType` | `nvarchar(100)` | NOT NULL | e.g. `"Employee"`. |
| `EntityId` | `uniqueidentifier` | NOT NULL | |
| `ActionType` | `nvarchar(50)` | NOT NULL | `AuditActionType` enum stored as string (`EmployeeCreated`, `EmployeeUpdated`, `EmployeeActivated`, `EmployeeDeactivated` — BR-AUD-003). |
| `Metadata` | `nvarchar(1000)` | NULL | Non-sensitive only (BR-AUD-004; identification numbers acceptable). |

Index: `IX_AuditLogs_EntityType_EntityId`. The insert-only DB grant on AuditLogs (no UPDATE/DELETE for the app identity) is a deployed-environment concern provisioned by Bicep — deferred to US-005/infra work; noted, not part of this plan.

No seed data.

---

## 4. Ordered implementation plan

Everything lives under `Code/` (user decision): `Code/evalux.sln`, `Code/src/`, `Code/tests/`, `Code/frontend/`, `Code/docker-compose.yml`. Git repo is initialized at the repo root; `.github/workflows/` sits at the repo root (GitHub requirement) with jobs running in `Code/`.

### Stage A — Greenfield bootstrap (must end functional: clean Release build, arch tests green, API boots to /swagger, frontend renders)

1. **[repo]** `git init` at repo root; `.gitignore` (dotnet + node); branch `feature/EVAL-002-manage-employees` (never work on `main`). `.editorconfig` + `Directory.Build.props` in `Code/` (net10.0, `Nullable=enable`, `TreatWarningsAsErrors=true`, implicit usings).
2. **[repo]** `Code/docker-compose.yml` — SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest`, port 1433) per tech-doc §4.3.
3. **[solution]** `Code/evalux.sln` with 6 `src/` + 4 `tests/` projects wired with **only** the allowed references (CLAUDE.md matrix; no `Evalux.Web` — React decision): `Evalux.SharedKernel`, `Evalux.Api`, `Evalux.Modules.{Employees,Criteria,Evaluations,Audit}`; `Evalux.{Unit,Integration,Arch,E2E}Tests`. Criteria and Evaluations start as empty skeletons (csproj + Contracts folder placeholder).
4. **[SharedKernel]** Base types: `Entities/BaseEntity.cs` (`BaseEntity<TId>`); `Contracts/IUnitOfWork.cs` (`SaveChangesAsync(CancellationToken)`); `Contracts/IAuditService.cs` + `AuditEntry` record (`ActionType`, `ActorId`, `EntityType`, `EntityId`, `Metadata`) + `AuditActionType` enum; `Contracts/IActorProvider.cs` (current-actor abstraction — see §6); `Exceptions/` — abstract `EvaluxException(message, errorCode)` → `NotFoundException`, `ConflictException`, `ValidationException(IReadOnlyList<ValidationFailure>)` (code-standards §4.6 naming + tech-doc HTTP mapping); `Common/PagedResult<T>.cs` (`Items`, `Total`, `Page`, `PageSize`); `Utilities/DecimalCalculator.cs` (round-to-2dp, `MidpointRounding.AwayFromZero` per code-standards).
5. **[Api]** Composition root skeleton: `Program.cs` (Serilog/structured logging, Swagger, CORS policy for `http://localhost:3000`, global exception handler, DI); `Infrastructure/EvaluxDbContext.cs` (applies entity configurations from each module assembly via `ApplyConfigurationsFromAssembly`); `Infrastructure/UnitOfWork.cs` (`IUnitOfWork` over the shared context); `Infrastructure/GlobalExceptionHandler.cs` (`IExceptionHandler` → RFC 7807 ProblemDetails: 404/409/422/403/500 map per tech-doc §6.2); DI alias `DbContext → EvaluxDbContext` so module repositories can inject the context without referencing Api (see §6); `await db.Database.MigrateAsync()` on startup inside an async scope (never `EnsureCreated`); `dotnet user-secrets init` + `ConnectionStrings:EvaluxDb` for local dev; `Infrastructure/DevActorProvider.cs` (fixed `"local-dev"` actor until US-001). **No authentication/authorization wiring** (user decision — see §6 risk note).
6. **[ArchTests]** `BoundaryRules.cs` (ArchUnitNET, xUnit): modules must not reference other modules' internals; SharedKernel depends on no module; service implementations reside in their owning module; endpoints never touch `EvaluxDbContext`/repositories directly; `Evalux.Modules.Evaluations` internals referenced by no other assembly.
7. **[repo]** CI workflow `.github/workflows/ci.yml` (working dir `Code/`): restore → Release build (zero warnings) → `dotnet format --verify-no-changes` → unit tests → arch tests → integration tests (Testcontainers) → coverage gates (domain/application ≥80%, overall ≥70%) → vulnerability + secret scanning. CD/Bicep is out of scope for this story.
8. **[frontend]** `Code/frontend/` — Next.js (TypeScript, App Router): app shell per mockups — dark top bar ("Evalux — Performance Evaluation · IGS Solutions"), left sidebar nav (Dashboard, Employees, Criteria, Evaluations, Audit trail), light-gray content canvas; placeholder pages for all five sections; typed API client (`lib/api.ts`: fetch wrapper, ProblemDetails/validation-error parsing, base URL from env `NEXT_PUBLIC_API_URL=https://localhost:7001`); `npx tsc --noEmit` clean.

### Stage B — US-002 backend (dependency order)

9. **[Modules.Audit]** `Domain/Entities/AuditLog.cs` (internal, fields per §3); `Infrastructure/Persistence/AuditLogEntityConfiguration.cs` (internal `IEntityTypeConfiguration<AuditLog>`); `Services/AuditService.cs` (internal sealed : `IAuditService`) — **stages** the `AuditLog` via the shared `DbContext`, never commits (atomic-audit invariant); public `AuditModuleRegistration.AddAuditModule(this IServiceCollection)`.
10. **[Modules.Employees]** `Domain/Entities/Employee.cs` — internal sealed `Employee : BaseEntity<Guid>`: private setters; static factory `Create(identificationNumber, fullName, email, jobTitle, department)` → Active + `CreatedAtUtc`; methods `Update(...)`, `Activate()`, `Deactivate()` (idempotence guards throw `ConflictException`); `Guid.CreateVersion7()` ids.
11. **[Modules.Employees]** `Contracts/` (the only public surface): `IEmployeeService` — `CreateAsync(CreateEmployeeCommand, ct)`, `UpdateAsync(UpdateEmployeeCommand, ct)`, `ActivateAsync(Guid id, ct)`, `DeactivateAsync(Guid id, ct)`, `GetByIdAsync(Guid id, ct)`, `SearchAsync(EmployeeSearchQuery, ct)` → `PagedResult<EmployeeDto>`; public records `EmployeeDto` (includes `IsActive` — FR-EM-010), `CreateEmployeeCommand`, `UpdateEmployeeCommand`, `EmployeeSearchQuery(searchTerm?, page, pageSize)`.
12. **[Modules.Employees]** `Infrastructure/Repositories/EmployeeRepository.cs` (internal, + internal `IEmployeeRepository`): `AddAsync`, `GetByIdAsync`, `ExistsByIdentificationNumberAsync(value, excludeId?)`, `SearchAsync` (partial match on `FullName` OR `IdentificationNumber`, **includes inactive** — AC-006; `AsNoTracking`, `CountAsync` + `Skip/Take` paging); never calls SaveChanges. `Infrastructure/Persistence/EmployeeEntityConfiguration.cs` per §3 schema.
13. **[Modules.Employees]** `Application/Validators/`: `CreateEmployeeCommandValidator`, `UpdateEmployeeCommandValidator` (FluentValidation): all 5 fields required with max lengths per §3, email format; **async uniqueness rule** on identification number via `IEmployeeRepository.MustAsync` → field-level message "An employee with this identification number already exists." (BR-EMP-001, AC-002/AC-007).
14. **[Modules.Employees]** `Services/EmployeeService.cs` (internal sealed : `IEmployeeService`), injecting `IEmployeeRepository`, `IUnitOfWork`, `IAuditService`, `IActorProvider`, `ILogger<T>`. Every mutation: apply domain change → `RecordActionAsync(AuditEntry)` → **single** `SaveChangesAsync` (atomic-audit). `DbUpdateException` on the unique index surfaces as `ConflictException` 409 (race backstop). `NotFoundException` for unknown ids. Public `EmployeesModuleRegistration.AddEmployeesModule(this IServiceCollection)`.
15. **[Api]** Register both modules + validators in `Program.cs`; **EF migration `Employees_InitialSchema`** (`dotnet ef migrations add Employees_InitialSchema --project src/Evalux.Api --startup-project src/Evalux.Api`, run from `Code/`).
16. **[Api]** `Endpoints/EmployeeEndpoints.cs` — `MapGroup("/api/v1/employees")` with a FluentValidation endpoint filter (expected input failures → 422 ValidationProblem, field-level, without exceptions-as-control-flow):
    - `POST /` → 201 + `EmployeeDto` (AC-001) · `GET /{id:guid}` → 200/404 (FR-EM-003) · `PUT /{id:guid}` → 200 (FR-EM-004) · `POST /{id:guid}/activate` → 200 (FR-EM-005) · `POST /{id:guid}/deactivate` → 200 (FR-EM-006) · `GET /?search=&page=&pageSize=` → 200 `PagedResult<EmployeeDto>` (FR-EM-009/010).
    - `.WithName(...)`, `.Produces<T>()`/`.ProducesProblem(...)` annotations. No business logic in endpoints. **No `RequireAuthorization` in this story** — each endpoint gets its policy when US-001 lands (`HRAdminPolicy` for mutations, `ViewerPolicy` for reads, per BR-SEC-004).

### Stage C — Frontend Employees page (per `Docs/UI mockups/screenshot 2.png`)

17. **[frontend]** `app/employees/page.tsx` + components: header ("Employees" / "The master record of everyone eligible for evaluation." / "+ New employee" primary button); search input ("Search by name or ID number", debounced, calls `GET /api/v1/employees?search=`); result count ("12 employees"); table — EMPLOYEE (initials avatar, name, email), ID NUMBER, TITLE, DEPARTMENT, STATUS (green-dot Active / gray-dot Inactive badge), actions.
18. **[frontend]** Row actions: red-outline **Deactivate** (active rows) / green-outline **Activate** (inactive rows) with a confirmation dialog (AC-004 says "the deactivation is confirmed"); an **Edit** action (added beyond the mockup — FR-EM-004 requires update; pattern borrowed from the Criteria screen, see §6); simple pager driven by `PagedResult` totals.
19. **[frontend]** Create/Edit modal form (5 fields; mockup doesn't render it — see §6): client-side required hints, server 422 errors mapped to field-level messages (AC-007), duplicate-ID error surfaced on the identification-number field (AC-002), success confirmation showing the new system-generated employee ID (AC-001).

### Stage D — Tests (per code-standards §9; naming `{Method}_Should{Expected}_When{Condition}`)

20. **[UnitTests]** `Modules/Employees/`: `EmployeeTests` (factory sets Active/CreatedAtUtc; Update/Activate/Deactivate transitions + guards), `EmployeeServiceTests` (audit entry staged **before** the exactly-once `SaveChangesAsync`; correct `AuditActionType` per operation; NotFound/Conflict paths), `CreateEmployeeCommandValidatorTests` (each required field, lengths, email format, uniqueness rule).
21. **[IntegrationTests]** `WebApplicationFactory<Program>` + Testcontainers SQL Server (migrations applied): create → 201 + Active + retrievable (AC-001); duplicate identification number → 422/409 with the BR message, no second record (AC-002); update persists (AC-003 — see UC-C note); deactivate → still searchable + `IsActive=false`, audit row written (AC-004, BR-AUD-003); partial-match search returns active+inactive (AC-006); missing field → 422 identifying the field, nothing persisted (AC-007); audit rows present for all four mutating actions.
22. **[E2ETests]** Playwright against Next.js + API + SQL: create-employee happy path incl. success confirmation with ID; duplicate-ID error visible; search finds inactive employee; deactivate/activate round-trip with confirmation dialog; missing-field validation display. ≥1 axe-core accessibility assertion on the Employees page (code-standards §7.3).

---

## 5. Use cases → acceptance criteria → tiers → verification

| Use case | AC / FR covered | DB | BE | FE | Verification |
|---|---|---|---|---|---|
| UC-A Create employee (Active, system ID, confirmation) | AC-001, AC-007 · FR-EM-001 | ✔ | ✔ | ✔ | API integration tests + Playwright flow |
| UC-B Reject duplicate identification number (active or inactive) | AC-002 · FR-EM-002, BR-022 | ✔ (unique index) | ✔ | ✔ (field error) | API integration test (validator + race backstop) + Playwright |
| UC-C Update employee | AC-003 · FR-EM-004 | — | ✔ | ✔ (edit modal) | API integration test + Playwright. The "finalized evaluations unaffected" half of AC-003 is **structural**: snapshots live in `EvaluationCriterionSnapshot` owned by Modules.Evaluations (US-006/US-009); no evaluation data exists yet. Fully verifiable only from US-006 on — flagged, not silently dropped. |
| UC-D Deactivate employee (soft, stays visible/searchable) | AC-004 · FR-EM-006/008, BR-017 | ✔ (`IsActive` flip) | ✔ | ✔ (confirm dialog, badge) | API integration test (record retained + searchable) + Playwright |
| UC-E Activate inactive employee | story goal · FR-EM-005 | — | ✔ | ✔ | API integration test + Playwright |
| UC-F Search by partial name / ID, returns active + inactive | AC-006 · FR-EM-009 | ✔ (indexes) | ✔ | ✔ | API integration test + Playwright |
| UC-G View employee record with status displayed | FR-EM-003/010 | — | ✔ (`GET /{id}`, `IsActive` in DTO) | ✔ (badge) | API integration test + Playwright |
| UC-H Inactive employee cannot receive evaluation | AC-005 · FR-EM-007, BR-003 | — | ✔ (contract surface only) | — | US-002 delivers the enforcement *input*: `IEmployeeService.GetByIdAsync` exposing `IsActive` — verified by unit + integration test. The rejection itself belongs to evaluation creation (US-006; traceability maps FR-EM-007 → UC-003). Full AC-005 verification happens in US-006 — explicit deferral, not a gap. |
| UC-I Employee actions audited atomically | BR-AUD-003 (supports US-005) | ✔ (`AuditLogs`) | ✔ | — | Unit test (staged-before-single-commit) + integration test (rows for all 4 actions) |

Every acceptance criterion is covered by at least one use case; every use case has a verification method. AC-003 (partially) and AC-005 carry explicit cross-story deferrals with their re-verification point (US-006).

## End-to-end completeness check (performed)

Each UC chain walked DB → service → endpoint → frontend → test: every endpoint has a consuming UI surface (UC-A/B create modal, UC-C edit modal, UC-D/E row actions, UC-F/G list page); every UI call has its endpoint (§4 item 16 covers all six); both tables have their migration (item 15); every validation has its UI error surface (422 → field-level, 409 → ID field, item 19); RBAC link intentionally absent per the no-auth decision (§6). No dangling links found.

---

## 6. Assumptions and decisions

**User-confirmed decisions (2026-07-17):**
1. **Solution root is `Code/`** — `Code/evalux.sln`, `Code/src/`, `Code/tests/`, `Code/frontend/`, `Code/docker-compose.yml`. Documented CLI commands run from `Code/`. `.github/workflows/` stays at repo root (GitHub requirement) targeting `Code/`.
2. **No auth in this story** — endpoints anonymous; US-001 retrofits JWT + policies later. ⚠️ Consequences accepted: the "RBAC on every endpoint" invariant and BR-SEC-004 are temporarily violated; the "authenticated HR Coordinator" premise of every AC and the DoD authorization checklist items cannot be verified until US-001; the audit `ActorId` uses a placeholder. Mitigations: endpoints structured for one-line policy addition; `IActorProvider` isolates actor resolution (US-001 swaps `DevActorProvider` for claims-based); risk logged here for the US-001 plan to pick up.
3. **Real minimal audit now** — `IAuditService` in SharedKernel, `AuditLog` + staging `AuditService` in Modules.Audit, single-commit atomicity. US-005 adds audit review endpoints/UI and remaining event types.
4. **All 5 employee fields required** (FR-EM-001 note read as "email and job title are required per HR operational policy"); lengths: IdentificationNumber 50, FullName 200, Email 254 (+format), JobTitle 100, Department 100.

**Planner decisions filling documented gaps (docs silent or conflicting):**
5. Per-module DI registration via public `Add{Module}Module()` extension methods (docs demand internal implementations + Api-only bindings but show no mechanism; this is the standard resolution; ArchUnitNET still guards boundaries).
6. Module repositories inject EF's `DbContext` base type; Api registers `EvaluxDbContext` as `DbContext` (modules cannot reference Api where the context lives; module csproj takes only the EF Core package).
7. Commands/queries/DTOs used by `IEmployeeService` are public records in `Contracts/` (a public interface cannot expose internal parameter types); validators stay internal in `Application/Validators/`.
8. Validation mechanism: FluentValidation endpoint filter returning 422 ValidationProblem (code-standards: no exceptions for expected control flow); DB unique index + `ConflictException` (409) as the concurrency backstop for identification-number uniqueness.
9. Code-standards wins its conflicts with tech-doc: `DateTimeOffset.UtcNow`, `EvaluxException` base, `MidpointRounding.AwayFromZero`, coverage gates 80/80/70; migrations folder `src/Evalux.Api/Infrastructure/Migrations/` (tech-doc §5.1, the more specific spec).
10. UI deltas from the mockup (which omits them but the ACs require them): an **Edit** row action + edit modal (FR-EM-004), a create/edit modal form with the 5 fields (mockup shows only the "+ New employee" button), a confirmation dialog on deactivate/activate (AC-004 "the deactivation is confirmed"), and a simple pager (backend contract is `PagedResult<T>`; mockup shows only a scrolling list with a count).
11. Out of scope here: CD pipeline/Bicep, the AuditLogs insert-only DB grant (deployed-env, Bicep — US-005/infra), Criteria/Evaluations module content (skeleton projects only), any US-001 acceptance criteria.

---

## Verification (story-level)

From `Code/`: `docker-compose up -d` → `dotnet build -c Release` (zero warnings) → `dotnet test` (unit + integration + arch suites green) → `dotnet watch --project src/Evalux.Api` (API on :7001, Swagger up) → `npm run dev` in `frontend/` (:3000) → Playwright E2E suite green. Manual smoke: create → search inactive → deactivate/activate → duplicate-ID rejection → audit rows in `dbo.AuditLogs`.

**Hand-off:** implement with `/implement .claude/implementation plans/US-002-implementation-plan.md`.
