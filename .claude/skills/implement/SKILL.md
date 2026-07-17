---
name: implement
description: Use when the user asks to implement something — a user story, feature, or an implementation plan. Handles BOTH cases; an existing plan (file path, .claude/User story implementation plan.md, or pasted text) or a request with no plan yet (take the story from a file, an ADO work item, or a plain description; derive the plan first, ask ambiguities, get approval). On a greenfield repo (no code yet) it first bootstraps the entire working solution from scratch following the tech guidelines, then implements. Drives the full house workflow; layered implementation under the modular-monolith boundary rules → EF migrations → two /code-review rounds with fixes → two /security-review rounds with fixes → Playwright verification of web use cases → session report. Triggers; "implementa", "implement", "implementemos el plan", "desarrolla la US/HU".
argument-hint: [plan file | user story | feature description]
model: claude-opus-4-8
effort: high
---

# Note: MANDATORY - Everything is in English: communication with the user, the plan document, reports, and all code, table/column names, comments, and identifiers.

# Implement — house implementation workflow

End-to-end workflow for implementing features/user stories in Evalux. Communicate and write everything **in English** — user-facing output, code, comments, and identifiers.

## Phase 0 — Intake (plan or no plan)

1. Review project memory and the root `CLAUDE.md` context (commands, gotchas, doc inconsistencies).
2. **If a plan exists** (path passed as argument, `.claude/User story implementation plan.md`, a `.claude/implementation plans/` file, or pasted text): read it **fully**. Extract the use cases and acceptance criteria; the plan is the source of truth — don't re-ask what it already answers.
3. **If no plan exists**: locate the user story first — in priority order: a file path passed as argument, an ADO work item id (fetch it with `mcp__ado__wit_get_work_item`: title, description, acceptance criteria), a US id from `Docs/backlog.md` (US-001…US-013), or a plain description given in the conversation (derive the acceptance criteria from it and confirm them with the user). Then build the plan before writing any code, following the house plan protocol:
   - State what the story/request means: **what, why, and what for**.
   - Show any tables to create or modify (schema-level, not just entity names).
   - Ordered list: what gets created, what gets modified, in dependency order.
   - Derive the **use cases** that satisfy each acceptance criterion.
   - Anything unclear, dual, or ambiguous → **ask the user; never invent or assume**.
   - Present the plan and get the user's approval before implementing.
4. **Branch preflight — always validate the current branch before writing any code.** Get it with `git rev-parse --abbrev-ref HEAD`. If the directory is not a git repository yet (greenfield), skip this preflight — Phase 0.5 initializes git and creates the feature branch:
   - **If the branch is `main`** (the protected base branch): do **NOT** implement here. Tell the user to switch to — or create — the correct feature branch for this work (`feature/EVAL-<id>-short-desc`), and **stop until they do**. Never implement directly on `main`.
   - **If the branch is anything else**: evaluate whether the branch name correlates with what is about to be implemented (the user story id/title/topic from the plan). If it clearly matches, continue. If there is **any** ambiguity or doubt about the correlation, **ask the user to confirm the current branch is the right one** before proceeding — never assume.
5. Load the use cases into TodoWrite — one item per use case — and keep it updated as you go.

## Phase 0.5 — Greenfield bootstrap (only when the codebase does not exist)

If the repository has no application code yet (no `evalux.sln`, no `src/`), **create everything from scratch and leave it functional** before implementing any use case, following the tech guidelines (`Docs/tech-doc.md` §4–§5, `Docs/code-standards.md`) and the binding `.claude/rules/`:

1. Initialize git if needed (`git init`, `main` as base branch, `.gitignore` for .NET + Node), then create the feature branch for this work.
2. Scaffold the solution: `evalux.sln` with the 7 `src/` projects (`Evalux.SharedKernel`, `Evalux.Api`, `Evalux.Modules.{Employees,Criteria,Evaluations,Audit}`) and the 4 `tests/` projects — wired with **only** the project references allowed by the matrix in root `CLAUDE.md`. `<Nullable>enable</Nullable>` everywhere; `.editorconfig` at the repo root with the analyzer severities from `Docs/code-standards.md` §12.
3. SharedKernel base types: `IUnitOfWork`, `IAuditService`, `IDomainEvent`, `BaseEntity<TId>`, `DecimalCalculator`, the `EvaluxException` hierarchy.
4. `Evalux.Api` skeleton: `Program.cs` composition root, `EvaluxDbContext` + `UnitOfWork`, global exception handler, Entra ID JWT authentication and the three RBAC policies, Key Vault provider (bypassed in `Development`), Swagger.
5. `Evalux.ArchTests` boundary rules **from day one** — the architecture must be self-enforcing before any feature code exists.
6. Local environment: `docker-compose.yml` (SQL Server 2022), `.env.example`, `dotnet user-secrets init`.
7. Next.js frontend under `frontend/` (TypeScript strict), following `.claude/rules/frontend.md` and the layout in `Docs/UI mockups/`.
8. CI workflow with the quality gates (build, tests, coverage thresholds, arch tests, secret scanning).

**Functional gate before proceeding:** `dotnet build` clean, `dotnet test` green (arch tests passing), the API boots and serves `/swagger`, and the frontend renders. Only then continue with Phase 1 for the actual use cases. If the scaffold itself is the session's goal, skip Phase 1–2 and continue from Phase 3 (reviews) so the scaffold still gets reviewed and reported.

## Phase 1 — Implementation (use case by use case)

- **Architecture is strictly binding**: the path-scoped rules in `.claude/rules/{sharedkernel,modules,api,tests,frontend}.md` load automatically and are law; the deep rationale is `Docs/software-architecture-document.md` (ADR-001…005) and `Docs/code-standards.md`. Decide placement first: shared abstraction → `Evalux.SharedKernel`; business rule/use case → the owning module (`Domain/` + `Services/`); a capability another module owns → consume that module's `Contracts/` interface via DI (never its concrete types); HTTP surface → `Evalux.Api` endpoints; UI → the Next.js frontend. Implement business rules against the `Docs/business-rules.md` catalog (BR-xxx-nnn). If the feature seems to require breaking a boundary rule, STOP and raise it.
- SOLID, constructor dependency injection (interfaces, never service locators/statics), high cohesion, low coupling, testable code. **No business logic in endpoints.**
- **Backend + frontend coverage**: any use case with a UI surface lands in both `Evalux.Api` (endpoints/services) and the Next.js frontend. Say explicitly when a backend change needs no frontend counterpart (and vice versa).
- Implement **one use case at a time**, announcing each as you start it and confirming when it's done (map it to its acceptance criterion). Don't batch-silently.
- Build gate as you advance. Gotcha: a running API can lock the module assemblies — kill the `dotnet` process serving `https://localhost:7001` before `dotnet build`/`test`/`ef`, or build the affected module project alone to typecheck.

## Phase 2 — Database migrations

- Create migrations in the correct dependency order so the **existing database never breaks**: additive changes first; guard index/FK drops with `IF EXISTS`; seeds idempotent; backfill instead of wipe. Destructive migrations require explicit Technical Lead approval in the PR.
- Commands (from the repo root; DbContext and migrations live in `src/Evalux.Api` — see root `CLAUDE.md`). Deployed environments apply migrations automatically at startup (`MigrateAsync()`); locally, run `dotnet ef database update --project src/Evalux.Api --startup-project src/Evalux.Api` explicitly against the dev DB and verify the app boots after.

## Phase 3 — Code review loop (exactly 2 rounds)

1. When the implementation is complete, invoke the **`/code-review` skill** over the working diff. Review focus: best practices, security, testability, readability, **no logic in endpoints**, dependency injection correctly applied, module boundary compliance, every class in its correct assembly and folder.
2. **Apply the corrections** it reports.
3. Invoke **`/code-review` again** to verify the fixes and re-review.
4. Apply any remaining fixes. **Stop after 2 rounds** — anything still open goes to the final report as a pending item, don't keep looping.

## Phase 4 — Security review loop (exactly 2 rounds)

Once the code-review rounds are done and their fixes applied:

1. Invoke the **`/security-review` skill** over the pending changes (injection, authZ/authN gaps, missing RBAC policies, sensitive data exposure in logs/responses, input validation, secrets, audit coverage of critical actions).
2. **Apply the corrections** it reports.
3. Invoke **`/security-review` again** to verify the fixes and re-review.
4. Apply any remaining fixes. **Stop after 2 rounds** — anything still open goes to the final report as a pending item.

## Phase 5 — Verification

- Run the backend test suite (`dotnet test` from the repo root — includes `Evalux.ArchTests` boundary rules), and `npx tsc --noEmit` in `frontend/` if it changed. Report real results — failures included.
- **Playwright E2E (web UI)**:
  1. Analyze the use cases and identify which are verifiable through the web UI.
  2. Ensure the API (`https://localhost:7001`) and the frontend (`:3000`) are running.
  3. Log in with the dev credentials you already know (project memory / seeded dev identity). **If you don't know them, ask the user — never guess or brute-force.**
  4. Exercise each identified use case end-to-end; take screenshots as evidence of pass/fail.

## Phase 6 — Session report

Deliver a final summary (in English) covering the whole session:

- Use cases implemented vs. acceptance criteria (table: use case → criterion → status).
- Files created/modified, grouped by assembly/frontend.
- Migrations created and whether they were applied to the dev DB.
- Review rounds (code review AND security review): findings reported, fixed, and anything left pending.
- Playwright results per use case (with evidence) and which use cases were not web-verifiable.
- Pending items: prod migration, deferred fixes, anything else left open.
