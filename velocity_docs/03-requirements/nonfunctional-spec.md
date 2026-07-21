# Non-Functional Requirements Specification

**Project:** VacaFlow_14
**Document ID:** NFR-001
**Reference:** SI-001 (Strategic Intake)
**Phase:** 03 — Requirements
**Author:** David Valdez, Laura Hernandez (AI Assisted)
**Date:** 2026-07-20
**Version:** 3.0
**Status:** Draft

---

## Executive Summary

This register documents the non-functional requirements for VacaFlow, an internal absence and vacation request management system developed for IGS Solutions. VacaFlow is scoped as a local MVP with a limited, controlled user population (IGS Solutions employees and their managers) running the application from source code on a reviewer machine.

Given the MVP nature and local execution context, NFR priorities are calibrated accordingly. Security and correctness are the dominant quality attributes — they are acceptance-gating. Usability and maintainability are addressed at a level appropriate for an internal tool. Performance at scale, high availability, and regulatory compliance are explicitly deferred pending a production promotion decision.

This document covers **23 non-functional requirements** across **8 quality attribute categories**. One category (Compliance) is formally acknowledged as out of scope for the MVP with deferred conditions recorded.

---

## NFR Summary

| Category | Count | Critical | High | Medium |
|----------|-------|----------|------|--------|
| Performance | 1 | — | — | 1 |
| Security | 5 | 5 | — | — |
| Availability | 2 | — | 1 | 1 |
| Usability | 3 | — | 2 | 1 |
| Reliability | 4 | 4 | — | — |
| Maintainability | 4 | — | 2 | 2 |
| Compatibility | 2 | — | 1 | 1 |
| Compliance | 2 | — | — | 2 |
| **Total** | **23** | **9** | **6** | **8** |

---

## 1. Performance Requirements

### NFR-PERF-001: End-to-End Reviewer Responsiveness
**Category:** Performance
**Priority:** Medium

#### Requirement
The application shall respond quickly enough that a single reviewer can complete the full end-to-end acceptance workflow — register, log in, create a Draft request, validate date rules, edit, submit, create a second Draft, cancel it, manager log in, approve or reject with comment, and view the final result — without perceptible blocking delays.

#### Acceptance Criteria
| Metric | Target | Threshold | Condition |
|--------|--------|-----------|-----------|
| Any user-initiated API call | < 2 seconds | < 5 seconds | Single user, local SQLite, no concurrent load |
| Page navigation / render | < 3 seconds | < 5 seconds | Standard modern browser on reviewer laptop |
| Database startup and migration | < 10 seconds | < 20 seconds | Cold start, no pre-existing database file |

#### Conditions
- User load: 1–5 concurrent users (reviewer team only, local execution)

  > ⚠️ **Note:** This concurrency estimate originates from SI-001 §5 (Assumptions), where the assumption that "SQLite is sufficient for the data volume and concurrency expected during local review (small number of concurrent users — reviewer team only)" is explicitly marked as **Not Validated**. It is also an open Critical Information Gap recorded in SI-001 §6 ("Number of employees expected to use the MVP during the review period"). This parameter should not be treated as a confirmed design baseline until those items are resolved.

- Network: Localhost loopback only; no public network involved
- Data volume: Seeded absence types, at most a few dozen requests created during review

#### Verification Method
- Manual walkthrough of the acceptance script; reviewer confirms no blocking delays
- No formal load testing tool is required for this MVP

#### Related Requirements
- SI-001 §5 Constraints: local execution from source code; no production load
- SI-001 §5 Assumptions: SQLite concurrency assumption (Not Validated — see Conditions note above)
- SI-001 §6 Critical Information Gaps: number of MVP users (open)

---

## 2. Security Requirements

### NFR-SEC-001: Password Storage
**Category:** Security
**Priority:** Critical

#### Requirement
The system shall never store user passwords in plain text. Every password shall be stored as a cryptographic hash using a recognized slow hashing algorithm before being persisted to the database.

#### Acceptance Criteria
- [ ] Registration endpoint applies a password hash before any write to the database
- [ ] The `Users` table (or equivalent) contains no plain-text password column or value when inspected post-registration
- [ ] Plain-text password storage is a hard rejection condition for acceptance
- [ ] Hashing algorithm used: BCrypt or equivalent (PBKDF2, Argon2); MD5 and SHA-1 are not acceptable

#### Verification Method
- Code review: confirm hashing call in the registration use case
- Post-registration database inspection: open the SQLite file with a viewer and confirm the stored value is a hash string

#### Related Requirements
- SI-001 §5 Constraints: "Passwords must be stored as hashes — plain text storage is a rejection condition"
- NFR-SEC-002 (Authentication)

---

### NFR-SEC-002: Session-Based Identity Derivation
**Category:** Security
**Priority:** Critical

#### Requirement
The API shall always derive the identity of the acting user (employee or manager) from the authenticated server-side session or token. The frontend shall never send an employee identifier, manager identifier, or approver identifier as a trusted business parameter in any request body or query string.

#### Acceptance Criteria
- [ ] All endpoints that operate on behalf of the current user read user identity exclusively from the server-side session or validated JWT claim
- [ ] No endpoint accepts a `userId`, `employeeId`, `managerId`, or `approverId` field in the request body as the source of authorization truth
- [ ] A request crafted with a forged or substituted user identifier in the body is rejected by the API
- [ ] Acceptance test: submitting a request with a manipulated user identifier in the payload produces a 401 or 403 response, not a successful business action on behalf of another user

#### Verification Method
- Code review: confirm all use cases read identity from `HttpContext.User` or validated token claims
- Penetration test (manual): attempt to forge an approver ID in an approval request body; confirm rejection

#### Related Requirements
- SI-001 §5 Constraints: the API must derive the current user and responsible approver from the authenticated session
- NFR-SEC-003 (Authorization)

---

### NFR-SEC-003: Role-Based Authorization Enforcement
**Category:** Security
**Priority:** Critical

#### Requirement
All ownership and role checks defined by the VacaFlow business rules shall be enforced by the API layer, independently of any UI state or frontend logic.

#### Acceptance Criteria
| Rule | Expected API Behavior |
|------|-----------------------|
| Only the request owner may edit, submit, or cancel their own request | API returns 403 if the authenticated user does not match the request owner |
| Only a user with the Manager role may approve or reject | API returns 403 if the authenticated user has the Employee role |
| A manager may only act on requests belonging to employees assigned to them | API returns 403 if the request does not belong to an employee under that manager — **Note:** this criterion depends on the one-to-one Manager-to-Employee assignment model (BR-013 / BR-DATA-002), which remains **pending formal confirmation** per SI-001 §6 Critical Information Gaps |
| A manager may not approve or reject their own request | API returns 403 if the request owner is the same as the authenticated manager |
| Only Submitted requests may be approved or rejected | API returns 400 or 422 if the request is not in Submitted state |
| Only Draft requests may be edited | API returns 400 or 422 if the request is not in Draft state |

- [ ] All rules above enforced at the API regardless of what the frontend sends
- [ ] UI not displaying invalid actions is treated as a UX improvement only, not a substitute for API enforcement
- [ ] Any bypass of these rules during acceptance review is an explicit rejection condition

#### Verification Method
- Code review: confirm each rule is checked in the application or domain layer, not only in the UI
- Manual testing: exercise each rule by calling the API directly with tools (e.g., Postman, curl) as an unauthorized actor; confirm 403/400/422 responses

#### Related Requirements
- SI-001 §4 Scope: business rules enforced server-side
- SI-001 §6 Critical Information Gaps: manager-to-employee assignment model (open — impacts routing and the third rule above)
- NFR-SEC-002 (Identity Derivation)
- NFR-REL-001 (State Transition Integrity)

---

### NFR-SEC-004: Database File Protection
**Category:** Security
**Priority:** Critical

#### Requirement
The SQLite database file shall not be publicly exposed and shall not be committed to source control containing real credentials or personal data.

#### Acceptance Criteria
- [ ] The repository `.gitignore` excludes the SQLite database file (e.g., `*.db`, `*.sqlite`)
- [ ] No database file with real usernames, emails, or hashed passwords is present in the committed codebase
- [ ] Seeded data used in development uses clearly non-real credential values (e.g., `dev@example.com`, placeholder names)
- [ ] The database file location is documented in the project README so a reviewer can manually back it up before any re-migration

#### Verification Method
- Repository inspection: confirm `.gitignore` entry and absence of committed database files
- Documentation review: confirm the README documents the database file path

#### Related Requirements
- SI-001 §5 Constraints: the database must not be committed with real passwords and must not be publicly exposed
- SI-001 §5 Assumptions: legal/regulatory deferred but basic data protection in force during MVP

---

### NFR-SEC-005: Authentication Session Management
**Category:** Security
**Priority:** Critical

#### Requirement
The system shall maintain an authenticated session or token across requests so that the user does not need to re-authenticate on every action, and so that the server can consistently identify the acting user throughout a workflow session.

#### Acceptance Criteria
- [ ] After a successful login, the user receives a session cookie or bearer token that is accepted by the API for subsequent requests
- [ ] Logout invalidates the session or token so that subsequent requests with the old credential are rejected
- [ ] No unauthenticated request may access any business endpoint (request creation, approval, etc.)
- [ ] Deferred: session timeout, MFA, password reset — these are not required for the MVP

#### Verification Method
- Manual test: log in, perform a business action, log out, attempt the same business action — confirm 401 after logout
- Code review: confirm middleware applies authentication to all business routes

#### Related Requirements
- SI-001 §4 Scope: Authentication endpoints (register, login, logout, current-user)
- NFR-SEC-002 (Identity Derivation)

---

## 3. Availability Requirements

### NFR-AVAIL-001: MVP Local Availability
**Category:** Availability
**Priority:** High

#### Requirement
The application shall start successfully from source code and remain operational throughout a review session without unplanned crashes or data loss that would interrupt the acceptance workflow.

#### Acceptance Criteria
| Scenario | Requirement |
|----------|-------------|
| Cold start (no existing database) | Application starts, migrates, and seeds data without manual intervention |
| Database already exists | Application starts and re-uses existing data without re-seeding or overwriting |
| Reviewer session (1–5 users, local) | Application remains operational for the duration of the acceptance demonstration — **Note:** the 1–5 user estimate derives from SI-001 §5 Assumptions (marked Not Validated) and SI-001 §6 (open Critical Information Gap); it is not a confirmed design parameter |
| Crash during review | Constitutes a blocking defect; must be resolved before acceptance |

- No formal uptime SLA, RTO, or RPO is defined for this MVP
- Automated backups are out of scope; the database file location is documented for manual backup

#### Verification Method
- Acceptance walkthrough: reviewer starts from a clean state; application completes full workflow without crash or restart

#### Related Requirements
- SI-001 §5 Constraints: blocking defects found during the review window must be resolved before final acceptance
- SI-001 §5 Assumptions: SQLite concurrency assumption (Not Validated)
- SI-001 §6 Critical Information Gaps: number of MVP users (open)

---

### NFR-AVAIL-002: Blocking Defect Resolution
**Category:** Availability
**Priority:** Medium

#### Requirement
Any defect that prevents the completion of the end-to-end acceptance workflow (register, log in, create Draft, validate, edit, submit, create a second Draft, cancel it, manager approve/reject, view result) shall be classified as a blocking defect and resolved before final acceptance sign-off. Cosmetic issues may be deferred.

#### Acceptance Criteria
| Defect Type | Classification | Resolution Requirement |
|-------------|----------------|------------------------|
| Cannot complete any step of the acceptance workflow | Blocking | Must be fixed before acceptance |
| Data is lost or corrupted during a normal workflow step | Blocking | Must be fixed before acceptance |
| UI element misaligned, label misspelled, minor visual issue | Cosmetic | May be deferred |
| Feature not in the agreed scope is missing | Out of scope | Not a defect |

#### Verification Method
- Live end-to-end acceptance session per SI-001 §5 Constraints (Business Constraints)

#### Related Requirements
- SI-001 §5 Constraints: blocking defects found during the review window must be resolved before final acceptance

---

## 4. Usability Requirements

### NFR-USE-001: Absence of Invalid Actions in UI
**Category:** Usability
**Priority:** High

#### Requirement
The user interface shall not display actions that are invalid for the current request state. Displaying a disabled action button is acceptable; displaying no button is preferred where context is clear.

#### Acceptance Criteria
| State | Employee View | Manager View |
|-------|--------------|--------------|
| Draft | Edit, Submit, Cancel visible | Not visible in manager queue |
| Submitted | Cancel visible; Edit not visible | Approve, Reject visible |
| Approved | No action buttons | No action buttons |
| Rejected | No action buttons | No action buttons |
| Cancelled | No action buttons | Not visible in manager queue |

- [ ] No action button is shown that would result in a guaranteed 403/400 API response for the current user and state combination
- [ ] Note: the API enforces these rules regardless; the UI check is a usability improvement only

#### Verification Method
- Manual review: walk through all states with both Employee and Manager accounts; confirm no misleading action buttons appear

#### Related Requirements
- NFR-SEC-003 (API enforcement is the authoritative gate)

---

### NFR-USE-002: Readable Forms and Labels
**Category:** Usability
**Priority:** High

#### Requirement
All forms and labels shall use clear, descriptive English text. The interface shall be compact and functional, appropriate for an internal tool used by a small team.

#### Acceptance Criteria
- [ ] All form fields have visible labels
- [ ] Required fields are visually indicated
- [ ] Validation error messages are human-readable and describe what is wrong (e.g., "End date cannot be before start date" rather than a raw error code)
- [ ] Date fields display dates in a consistent, unambiguous format (YYYY-MM-DD or locale-equivalent)
- [ ] No cryptic identifiers (GUIDs, database IDs) are displayed to users as primary labels

#### Verification Method
- Manual review during acceptance walkthrough
- Reviewer confirms forms are usable without additional explanation

#### Related Requirements
- SI-001 §4 Scope: Web screens listed

---

### NFR-USE-003: Accessibility Baseline
**Category:** Usability
**Priority:** Medium

#### Requirement
The application shall not be actively difficult to use by someone navigating with a keyboard or with high browser zoom. Formal WCAG 2.1 certification is not required for the MVP.

#### Acceptance Criteria
- [ ] All interactive elements (buttons, inputs, links) are reachable via keyboard Tab navigation
- [ ] Form submission is possible via keyboard (Enter key or Tab to submit button)
- [ ] The application is usable at 150% browser zoom without critical layout breakage
- [ ] Formal screen reader testing and color contrast audits are deferred

#### Verification Method
- Manual check: reviewer navigates the main workflow using keyboard only and confirms usability

---

## 5. Reliability Requirements

### NFR-REL-001: State Transition Integrity
**Category:** Reliability
**Priority:** Critical

#### Requirement
The system shall enforce all defined request state transitions and reject invalid transitions, ensuring that no request can reach an invalid state through any combination of API calls.

#### Acceptance Criteria

**Valid transitions (must succeed):**
| From | To | Actor |
|------|----|-------|
| Draft | Submitted | Request owner (Employee) |
| Draft | Cancelled | Request owner (Employee) |
| Submitted | Approved | Assigned Manager |
| Submitted | Rejected | Assigned Manager |
| Submitted | Cancelled | Request owner (Employee) |

**Invalid transitions (must be rejected with 400 or 422):**
| Attempted Transition | Expected API Response |
|----------------------|----------------------|
| Submitted → Submitted (re-submit) | 400 / 422 |
| Approved → any state | 400 / 422 |
| Rejected → any state | 400 / 422 |
| Cancelled → any state | 400 / 422 |
| Draft → Approved (skip submit) | 400 / 422 |
| Draft → Rejected (skip submit) | 400 / 422 |

- [ ] State is stored authoritatively in the database; UI state is not the source of truth
- [ ] No combination of concurrent or sequential API calls can leave a request in an undefined state

#### Verification Method
- Code review: confirm state machine or equivalent guard logic in the domain or application layer
- Manual API testing: attempt invalid transitions directly; confirm rejection responses

#### Related Requirements
- NFR-SEC-003 (Role enforcement)
- SI-001 §4 Scope: full request lifecycle

---

### NFR-REL-002: Date Validation Integrity
**Category:** Reliability
**Priority:** Critical

#### Requirement
The system shall enforce date validation rules for absence requests at the API layer, rejecting any request that violates them regardless of frontend input.

#### Acceptance Criteria
| Rule | API Behavior |
|------|-------------|
| End date is before start date | Rejected with 400 / 422 and descriptive error |
| Start date is in the past (relative to submission date) | Rejected with 400 / 422 and descriptive error |
| End date equals start date | Accepted (single-day absence) |

- [ ] Validation applied on creation and on edit (Draft state)
- [ ] Error messages identify which date field violated which rule
- [ ] Frontend date pickers showing these constraints are a UX aid only; API validation is authoritative

#### Verification Method
- API testing: submit requests with invalid dates directly; confirm rejection and error messages
- Acceptance walkthrough: create a request with past start date; confirm rejection

#### Related Requirements
- SI-001 §4 Scope: "end date cannot precede start date; start date cannot be in the past"

---

### NFR-REL-003: Approval Record Integrity
**Category:** Reliability
**Priority:** Critical

#### Requirement
Every approval or rejection decision shall produce exactly one Approval record in the database, associating the authenticated manager, the decision outcome, the decision date, and an optional comment.

#### Acceptance Criteria
- [ ] An Approved or Rejected request has exactly one corresponding Approval record
- [ ] The Approval record references the authenticated manager's identity (not a value passed from the frontend)
- [ ] No request can be approved or rejected more than once
- [ ] Attempting to approve an already-approved or already-rejected request returns 400 / 422
- [ ] An optional comment field is accepted and persisted if provided; null is accepted if omitted

#### Verification Method
- Post-approval database inspection: confirm one Approval row per decision
- Code review: confirm Approval record creation uses server-derived manager identity

#### Related Requirements
- NFR-SEC-002 (Identity derivation)
- NFR-SEC-003 (Manager authorization)
- SI-001 §2 Value Proposition: every approval or rejection creates one Approval record

---

### NFR-REL-004: Data Persistence on Restart
**Category:** Reliability
**Priority:** Critical

#### Requirement
All data created during a review session (users, requests, approvals) shall persist in the SQLite database and be available after the application is restarted, provided the database file is not deleted.

#### Acceptance Criteria
- [ ] Restart the API without deleting the database file; all previously created requests and approvals are still accessible
- [ ] EF Core migration on startup does not drop or re-seed data if the database already exists and schema is current
- [ ] If the schema requires migration (new migration applied), existing data is preserved or migration is additive

#### Verification Method
- Manual test: create data, stop the API, restart the API, verify data is present via the UI or a direct API call

#### Related Requirements
- NFR-AVAIL-001 (Startup behavior)
- SI-001 §5 Constraints: automatic migration on startup

---

## 6. Maintainability Requirements

### NFR-MAINT-001: Onion Architecture Layer Separation
**Category:** Maintainability
**Priority:** High

#### Requirement
The codebase shall follow a reduced Onion Architecture with clear layer separation: Domain, Application, Infrastructure, Api, and Web. Cross-layer dependency rules shall be enforced directionally (inner layers do not depend on outer layers).

#### Acceptance Criteria
| Layer | Permitted Dependencies | Prohibited Dependencies |
|-------|----------------------|------------------------|
| Domain | None | Application, Infrastructure, Api, Web |
| Application | Domain | Infrastructure, Api, Web |
| Infrastructure | Domain, Application | Api, Web |
| Api | Domain, Application, Infrastructure | Web |
| Web | None (separate Next.js project) | Any backend layer directly |

- [ ] Domain layer contains entities and business rules only; no EF Core, no HTTP, no UI concerns
- [ ] Application layer contains use case orchestration; references domain interfaces, not infrastructure implementations
- [ ] Infrastructure layer contains EF Core DbContext, repository implementations, and migration files
- [ ] Api layer contains Minimal API endpoint registrations; delegates to application use cases
- [ ] Web layer is a Next.js/React application that communicates with the API over HTTP only

#### Verification Method
- Code review: confirm project references and namespace dependencies enforce the above rules
- No circular project references permitted

#### Related Requirements
- SI-001 §5 Constraints: reduced Onion Architecture (Domain, Application, Infrastructure, Api, Web layers)

---

### NFR-MAINT-002: Avoidance of Unnecessary Abstraction Patterns
**Category:** Maintainability
**Priority:** High

#### Requirement
The codebase shall not introduce additional abstraction layers or frameworks that add indirection without proportionate benefit at the MVP scale. The stack is constrained to ASP.NET Core Minimal API, EF Core, and Next.js; patterns such as mediator dispatch pipelines, generic repository abstractions, event bus frameworks, or CQRS segregation frameworks are explicitly prohibited.

#### Acceptance Criteria
- [ ] No MediatR package reference in any project
- [ ] No `IRepository<T>` or generic repository pattern; repositories (if used) are use-case-specific interfaces
- [ ] No event bus, service bus, or message broker
- [ ] No CQRS command/query segregation framework
- [ ] Use case logic is directly callable from API endpoints without a mediator dispatch layer

#### Verification Method
- Code review: inspect NuGet package references and project structure; confirm absence of prohibited packages and patterns

#### Related Requirements
- SI-001 §5 Constraints: reduced Onion Architecture; stack constrained to ASP.NET Core Minimal API, EF Core, Next.js

---

### NFR-MAINT-003: Test Coverage
**Category:** Maintainability
**Priority:** Medium

#### Requirement
Critical business rule logic (state transitions, date validation, authorization checks) shall have automated unit or integration test coverage sufficient to detect regressions during the MVP validation period.

#### Acceptance Criteria
| Component | Minimum Coverage Target |
|-----------|------------------------|
| Domain business rules (state machine, date validation) | ≥ 80% line coverage |
| Application use case authorization checks | ≥ 70% line coverage |
| API endpoint happy-path and primary error paths | At least one integration test per endpoint group |
| UI components | Not required for MVP |

- [ ] Tests can be run with a single command (`dotnet test`) without external dependencies
- [ ] All tests pass on a clean clone of the repository

#### Verification Method
- `dotnet test` with coverage reporting (e.g., coverlet); review coverage report for domain and application layers

---

### NFR-MAINT-004: Local Setup Documentation
**Category:** Maintainability
**Priority:** Medium

#### Requirement
A README or equivalent document shall enable a new reviewer to start both the API and the frontend from source code on a clean machine, using only the documented prerequisites, without additional verbal instructions.

#### Acceptance Criteria
- [ ] Prerequisites listed (Node.js version, .NET SDK version, no Docker required)
- [ ] Step-by-step commands to clone, restore dependencies, and run the API
- [ ] Step-by-step commands to install frontend dependencies and start the Next.js dev server
- [ ] SQLite database file location documented (for manual backup)
- [ ] Any seeded accounts (manager email, password) documented for reviewer use
- [ ] Document reviewed and validated by at least one person who was not involved in writing it

#### Verification Method
- Dry run: a team member follows the README on a clean machine and confirms the application starts and is usable without additional guidance

#### Related Requirements
- SI-001 §5 Assumptions: development team has local environments capable of running Next.js and ASP.NET Core simultaneously
- NFR-AVAIL-001 (Cold start)

---

## 7. Compatibility Requirements

### NFR-COMPAT-001: Local Execution Without External Infrastructure
**Category:** Compatibility
**Priority:** High

#### Requirement
The application shall run entirely from source code on a local machine without requiring Docker, Azure, any cloud service, CI/CD pipeline, or external database server.

#### Acceptance Criteria
| Dependency | Required | Explicitly Excluded |
|------------|----------|---------------------|
| .NET SDK (version per README) | Yes | |
| Node.js / npm (version per README) | Yes | |
| SQLite (embedded via EF Core) | Yes | |
| Docker | | Excluded |
| Azure or any cloud service | | Excluded |
| SQL Server, PostgreSQL, or other server database | | Excluded |
| CI/CD pipeline (GitHub Actions, Azure DevOps) | | Excluded |

- [ ] Running `dotnet run` in the Api project starts the backend
- [ ] Running `npm run dev` in the Web project starts the frontend
- [ ] No additional infrastructure is required beyond the two commands above (after initial `npm install` and `dotnet restore`)

#### Verification Method
- Acceptance environment: reviewer machine with only the documented prerequisites installed; application starts and runs the full workflow

#### Related Requirements
- SI-001 §5 Constraints: application must run locally from source code — no Azure, cloud hosting, Docker, or CI/CD in this MVP

---

### NFR-COMPAT-002: Browser Support
**Category:** Compatibility
**Priority:** Medium

#### Requirement
The Next.js frontend shall function correctly on modern versions of the browsers available on the reviewer's machine. No legacy browser support is required.

#### Acceptance Criteria
| Browser | Minimum Version | Support Level |
|---------|----------------|---------------|
| Google Chrome | Last 2 releases | Full |
| Microsoft Edge | Last 2 releases | Full |
| Mozilla Firefox | Last 2 releases | Full |
| Safari (macOS) | Last 2 releases | Full |
| Internet Explorer | Any | Not supported |
| Mobile browsers | Any | Not required for MVP |

- [ ] Acceptance demonstration is conducted on a supported browser
- [ ] No polyfills for IE or legacy browsers required

#### Verification Method
- Acceptance walkthrough conducted on any one of the supported browsers listed above

---

## 8. Compliance Requirements

### NFR-COMP-001: Data Privacy — MVP Scope Acknowledgment
**Category:** Compliance
**Priority:** Medium (acknowledged; deferred)

#### Requirement
The MVP stores basic employee identity data (name, email, hashed password) and absence request data (dates, reasons, approval comments). No formal regulatory compliance framework (GDPR, HIPAA, SOC 2) is required for the MVP. If VacaFlow is promoted to a production system, privacy policy, data retention rules, and formal compliance requirements must be formally assessed before promotion.

#### Current MVP Controls (Minimum)
- [ ] Database file not publicly exposed (covered by NFR-SEC-004)
- [ ] Database file not committed with real credentials (covered by NFR-SEC-004)
- [ ] No privacy notice or consent flow required in the MVP application
- [ ] No data retention schedule required for the MVP

#### Deferred Compliance Items
| Item | Deferred To |
|------|-------------|
| GDPR privacy notice and consent flow | Post-production promotion decision |
| Data retention and deletion policy | Post-production promotion decision |
| SOC 2 or equivalent audit | Post-production promotion decision |
| Formal data classification policy | Post-production promotion decision |

#### Verification Method
- Sponsor acknowledgment recorded in SI-001 §5 Assumptions (legal/regulatory assumption)

#### Related Requirements
- SI-001 §5 Assumptions: no GDPR, HIPAA, or equivalent regulatory framework applies to the storage of employee name, email, and absence reason data in this MVP context
- SI-001 §5 Constraints: Legal Constraints

---

### NFR-COMP-002: No Formal Audit Logging
**Category:** Compliance
**Priority:** Medium (acknowledged; deferred)

#### Requirement
Advanced audit logging (immutable event log, timestamp-stamped action history, export-ready audit trail) is explicitly out of scope for this MVP. The Approval record (manager identity, decision, date, comment) constitutes the only required audit artifact.

#### Current MVP Controls (Minimum)
- [ ] Each Approval record persists: request reference, manager identity (server-derived), decision (Approved/Rejected), decision date, optional comment
- [ ] This record is queryable by the reviewer as evidence of who approved or rejected a request

#### Deferred Items
| Item | Deferred To |
|------|-------------|
| Immutable event log for all user actions | Post-production promotion |
| Audit log export (CSV, PDF) | Post-production promotion |
| Login/logout audit trail | Post-production promotion |

#### Verification Method
- Post-approval database inspection: confirm Approval record fields are populated and correct

#### Related Requirements
- SI-001 §4 Scope: advanced audit logs beyond the core Approval record listed under Out of Scope

---

## NFR Traceability

| NFR ID | Description | Related FRs | Predecessor Reference | Architecture Component |
|--------|-------------|-------------|----------------------|------------------------|
| NFR-PERF-001 | Reviewer responsiveness | All acceptance workflow steps | SI-001 §5 Constraints, §5 Assumptions (Not Validated), §6 | API + SQLite |
| NFR-SEC-001 | Password hashing | Registration | SI-001 §5 Constraints | Infrastructure / Identity |
| NFR-SEC-002 | Session-based identity | All authenticated actions | SI-001 §5 Constraints | Api / Auth middleware |
| NFR-SEC-003 | Role-based authorization | Edit, Submit, Cancel, Approve, Reject | SI-001 §4 Scope, §6 (assignment model open) | Application layer |
| NFR-SEC-004 | Database file protection | — | SI-001 §5 Constraints | Infrastructure / DevOps |
| NFR-SEC-005 | Session management | Login, Logout | SI-001 §4 Scope | Api / Auth middleware |
| NFR-AVAIL-001 | Local availability | All workflow steps | SI-001 §5 Constraints, §5 Assumptions (Not Validated), §6 | Api + SQLite + Web |
| NFR-AVAIL-002 | Blocking defect resolution | All acceptance steps | SI-001 §5 Constraints | Cross-cutting |
| NFR-USE-001 | No invalid actions in UI | All state-dependent actions | SI-001 §4 Scope | Web layer |
| NFR-USE-002 | Readable forms | Registration, request creation/edit | SI-001 §4 Scope | Web layer |
| NFR-USE-003 | Accessibility baseline | All UI screens | — | Web layer |
| NFR-REL-001 | State transition integrity | Submit, Cancel, Approve, Reject | SI-001 §4 Scope | Domain / Application |
| NFR-REL-002 | Date validation integrity | Request creation and edit | SI-001 §4 Scope | Domain / Application |
| NFR-REL-003 | Approval record integrity | Approve, Reject | SI-001 §2 Value Proposition | Domain / Application / Infrastructure |
| NFR-REL-004 | Data persistence on restart | All data-creating operations | SI-001 §5 Constraints | Infrastructure / EF Core |
| NFR-MAINT-001 | Onion architecture layers | — | SI-001 §5 Constraints | All layers |
| NFR-MAINT-002 | No unnecessary abstraction patterns | — | SI-001 §5 Constraints | All layers |
| NFR-MAINT-003 | Test coverage | Business rule logic | — | Domain / Application |
| NFR-MAINT-004 | Local setup documentation | — | SI-001 §5 Assumptions | DevOps / README |
| NFR-COMPAT-001 | Local execution | — | SI-001 §5 Constraints | Infrastructure / DevOps |
| NFR-COMPAT-002 | Browser support | All UI screens | — | Web layer |
| NFR-COMP-001 | Data privacy acknowledgment | Registration, personal data | SI-001 §5 Assumptions | Infrastructure / Data |
| NFR-COMP-002 | No formal audit logging | Approval | SI-001 §4 Scope | Domain / Infrastructure |

---

## Approval

| Role | Name | Date | Signature / Status |
|------|------|------|--------------------|
| Solution Architect | | | Pending |
| Technical Lead | | | Pending |
| QA Lead | | | Pending |
| Operations Lead | | | Pending |
| Business Sponsor | | | Pending |

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-07-18 | David Valdez (AI Assisted) | Initial draft — 23 NFRs across 8 categories calibrated to VacaFlow MVP local execution context |
| 2.0 | 2026-07-18 | David Valdez, Laura Hernandez (AI Assisted) | Corrected all SI-001 section cross-references; reformulated NFR-MAINT-002 to remove non-existent quoted text; unified availability citations; corrected NFR Summary table totals |
| 3.0 | 2026-07-20 | David Valdez, Laura Hernandez (AI Assisted) | Applied reviewer feedback: added Not Validated concurrency note and SI-001 §6 open gap reference to NFR-PERF-001 Conditions and NFR-AVAIL-001; added pending-confirmation note for Manager-to-Employee assignment model dependency in NFR-SEC-003; updated traceability table to reflect new cross-references |

---
## Document Control

| Field | Value |
|-------|-------|
| Author | David Valdez, Laura Hernandez (AI Assisted) |
| Approval Authority | Solution Architect (PM_OVERRIDE — bypassed Solution Architect) |
| Status | Approved |
| Signature | ✅ SIGNED by Laura Hernandez (laura.hernandez@arroyoconsulting.net) on 2026-07-20 16:14:09 UTC |

*— End of document —*
