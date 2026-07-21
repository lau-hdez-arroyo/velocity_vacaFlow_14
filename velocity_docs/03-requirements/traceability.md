# Requirements Traceability Matrix

**Project:** VacaFlow_14
**Document ID:** RTM-001
**Reference:** FRS-001 (Functional Requirements Specification), NFR-001 (Non-Functional Requirements Specification), BR-001 (Business Rules Catalog)
**Phase:** 03 — Requirements
**Author:** David Valdez, Laura Hernandez (AI Assisted)
**Date:** 2026-07-21
**Version:** 5.0
**Status:** Draft

---

## Executive Summary

This Requirements Traceability Matrix (RTM) establishes bidirectional links between all requirements defined for VacaFlow_14 and their corresponding design elements, implementation components, and test cases. The matrix covers **29 functional requirements** (grouped across 4 feature modules), **23 non-functional requirements** (across 8 quality attribute categories), **21 business rules** (from the authoritative Business Rules Catalog BR-001 v1.2), and **13 use cases**.

Given that VacaFlow is an MVP at the requirements phase — prior to formal architecture and test case authoring — design and test references are recorded as architectural component allocations and anticipated test coverage areas rather than finalized artifact IDs. These links will be validated and refined as the Software Architecture Document (SAD) and Test Cases documents are produced.

**Coverage at time of issue:**

| Requirement Category | Total | With Component Allocation | With Test Coverage Planned |
|----------------------|-------|--------------------------|---------------------------|
| Functional (Must Have) | 29 | 29 | 29 |
| Non-Functional | 23 | 23 | 23 |
| Business Rules | 21 | 21 | 21 |
| Use Cases | 13 | 13 | 13 |
| **Total** | **86** | **86** | **86** |

---

## 1. Overview

### 1.1 Purpose

This RTM provides a single structured view of how every requirement in VacaFlow_14 flows from its business source through functional specification, system design, implementation component, and test verification. It serves four primary purposes:

1. **Coverage verification** — confirm every requirement has a designated implementation component and a planned test
2. **Change impact analysis** — determine which components and tests are affected when any requirement changes
3. **Gap identification** — surface requirements with no design or test coverage
4. **Compliance traceability** — demonstrate that acceptance conditions stated in the Strategic Intake (SI-001) are covered end-to-end

### 1.2 Traceability Levels

```
SI-001 (Strategic Intake)
        │
        ▼
Functional Requirements (FR-###)
Non-Functional Requirements (NFR-###)
Business Rules (BR-###)
        │
        ├──────────────────────┐
        ▼                      ▼
Architecture Components    Test Cases
(Layer / Service)          (TC-###)
        │                      │
        ▼                      │
Implementation         ◀───────┘
(Source Code Module)
```

### 1.3 Document References

| Document | Document ID | Version | Status |
|----------|-------------|---------|--------|
| Strategic Intake | SI-001 | 1.0 | Approved |
| Functional Requirements Specification | FRS-001 | 1.2 | Approved |
| Non-Functional Requirements Specification | NFR-001 | 3.0 | Approved |
| Business Rules Catalog | BR-001 | 1.2 | Approved |
| Software Architecture Document | SAD-001 | — | Not Yet Produced |
| Test Cases | TC-DOC-001 | — | Not Yet Produced |

### 1.4 Architecture Layer Reference

VacaFlow uses a reduced Onion Architecture. All component allocations in this RTM refer to the following layers:

| Layer Label | Description |
|-------------|-------------|
| Domain | Entities, value objects, state machine, business rules |
| Application | Use case orchestration, authorization checks, validation |
| Infrastructure | EF Core DbContext, repository implementations, migrations, seeding |
| Api | ASP.NET Core Minimal API endpoint registrations, auth middleware |
| Web | Next.js / React frontend |

---

## 2. Functional Requirements Traceability

### 2.1 Authentication and User Registration (FR-AUTH)

| FR ID | Requirement Summary | Priority | Source (SI-001) | Architecture Layer(s) | Planned Test Coverage | Notes |
|-------|--------------------|-----------|-----------------|-----------------------|----------------------|-------|
| FR-AUTH-001 | User registers with name, email, password, and role | Must Have | §4 In Scope | Api, Application, Infrastructure | TC-AUTH-P-001 (valid registration), TC-AUTH-N-001 (duplicate email), TC-AUTH-N-002 (missing fields) | Employee-to-Manager assignment established separately (FR-AUTH-010) |
| FR-AUTH-002 | Passwords stored as cryptographic hashes | Must Have | §5 Technical Constraints | Infrastructure / Identity | TC-AUTH-P-002 (hash verification post-registration), TC-AUTH-N-003 (plain-text absence check) | Hard rejection condition per SI-001 |
| FR-AUTH-003 | Registered user can log in with email and password | Must Have | §4 In Scope | Api, Application, Infrastructure | TC-AUTH-P-003 (valid credentials), TC-AUTH-N-004 (wrong password), TC-AUTH-N-005 (unknown email) | |
| FR-AUTH-004 | Authenticated user can log out, terminating session | Must Have | §4 In Scope | Api, Application | TC-AUTH-P-004 (logout terminates session), TC-AUTH-N-006 (post-logout request rejected) | |
| FR-AUTH-005 | Current-user endpoint returns identity and role from session | Must Have | §5 Technical Constraints | Api, Application | TC-AUTH-P-005 (authenticated call returns identity), TC-AUTH-N-007 (unauthenticated returns 401) | Frontend must never supply identity |
| FR-AUTH-006 | All business endpoints require a valid authenticated session | Must Have | §5 Technical Constraints | Api (auth middleware) | TC-AUTH-N-008 (unauthenticated requests rejected for all business endpoints) | |
| FR-AUTH-007 | At least one Manager account seeded on startup | Must Have | §5 Technical Constraints | Infrastructure (seeding) | TC-AUTH-P-006 (seeded manager can log in on cold start) | Required for acceptance demonstration |
| FR-AUTH-008 | Three absence types seeded on startup | Must Have | §4 In Scope | Infrastructure (seeding) | TC-AUTH-P-007 (absence type catalog present on cold start) | Vacation, Personal Leave, Sick Leave |
| FR-AUTH-009 | Automatic database migration on startup | Must Have | §5 Technical Constraints | Infrastructure (EF Core) | TC-AUTH-P-008 (cold start produces migrated schema and seeded data) | No manual step permitted |
| FR-AUTH-010 | Employee record stores single assigned Manager reference; set via seed/controlled setup | Must Have | Meeting transcript; pending SI-001 §6 resolution | Infrastructure (seeding), Domain | TC-AUTH-P-009 (employee record has manager reference post-seed), TC-AUTH-N-009 (non-assigned manager cannot approve employee's request) | BR-DATA-002; pending James Parker confirmation |

### 2.2 Employee Request Management (FR-EMP)

| FR ID | Requirement Summary | Priority | Source (SI-001) | Architecture Layer(s) | Planned Test Coverage | Notes |
|-------|--------------------|-----------|-----------------|-----------------------|----------------------|-------|
| FR-EMP-001 | Employee creates Draft request with absence type, dates, and reason | Must Have | §4 In Scope | Api, Application, Domain, Infrastructure | TC-EMP-P-001 (valid creation), TC-EMP-N-001 (missing fields) | |
| FR-EMP-002 | Employee edits own Draft request | Must Have | §4 In Scope | Api, Application, Domain, Infrastructure | TC-EMP-P-002 (valid edit), TC-EMP-N-002 (edit on non-Draft rejected) | Only Draft state is editable (BR-REQ-003) |
| FR-EMP-003 | Employee submits own Draft request | Must Have | §4 In Scope | Api, Application, Domain, Infrastructure | TC-EMP-P-003 (submit transitions to Submitted), TC-EMP-N-003 (submit non-Draft rejected) | |
| FR-EMP-004 | Employee cancels own Draft or Submitted request | Must Have | §4 In Scope; FRS-001 §3.2 FR-EMP-004 | Api, Application, Domain, Infrastructure | TC-EMP-P-004 (cancel Draft → Cancelled), TC-EMP-P-008 (cancel Submitted → Cancelled), TC-EMP-N-004 (cancel Approved/Rejected/Cancelled rejected) | Cancel applies to Draft and Submitted (BR-REQ-005) |
| FR-EMP-005 | Submitted and final-state requests are read-only for employee | Must Have | §4 In Scope | Api, Application, Domain | TC-EMP-N-005 (edit Submitted rejected), TC-EMP-N-006 (edit Approved rejected), TC-EMP-N-007 (state change on final states rejected) | |
| FR-EMP-006 | Employee views own request list with state and final decision | Must Have | §4 In Scope | Api, Application, Infrastructure | TC-EMP-P-005 (list returns all own requests), TC-EMP-P-006 (decided requests show decision and comment) | |
| FR-EMP-007 | End date cannot be earlier than start date | Must Have | §5 Business Constraints | Domain, Application | TC-EMP-N-008 (end before start rejected), TC-EMP-P-007 (same-day request accepted) | BR-REQ-001; API-enforced |
| FR-EMP-008 | Start date cannot be in the past | Must Have | §5 Business Constraints | Domain, Application | TC-EMP-N-009 (past start date rejected) | BR-REQ-002; API-enforced |

### 2.3 Manager as Requester (FR-MGR-REQ)

| FR ID | Requirement Summary | Priority | Source (SI-001) | Architecture Layer(s) | Planned Test Coverage | Notes |
|-------|--------------------|-----------|-----------------|-----------------------|----------------------|-------|
| FR-MGR-REQ-001 | Manager creates, edits, submits, and cancels own requests under Employee rules | Must Have | §4 In Scope | Api, Application, Domain, Infrastructure | TC-MGR-P-001 (full employee-side lifecycle for manager), TC-MGR-N-001 (same date rule violations apply) | All FR-EMP business rules apply; Cancel applies to Draft and Submitted per FRS-001 FR-EMP-004 and BR-REQ-005 |
| FR-MGR-REQ-002 | Manager cannot approve or reject their own request | Must Have | §5 Business Constraints | Application, Domain | TC-MGR-N-002 (self-approval returns error, no Approval record created) | BR-MGR-004; enforced at API |

### 2.4 Manager Approval and Rejection (FR-APPR)

| FR ID | Requirement Summary | Priority | Source (SI-001) | Architecture Layer(s) | Planned Test Coverage | Notes |
|-------|--------------------|-----------|-----------------|-----------------------|----------------------|-------|
| FR-APPR-001 | Manager views Submitted requests from assigned employees only | Must Have | §4 In Scope | Api, Application, Infrastructure | TC-APPR-P-001 (review list contains only assigned employees' Submitted requests), TC-APPR-N-001 (requests from non-assigned employees not visible) | Depends on FR-AUTH-010 / BR-DATA-002 |
| FR-APPR-002 | Manager approves Submitted request | Must Have | §4 In Scope | Api, Application, Domain, Infrastructure | TC-APPR-P-002 (approve transitions to Approved, Approval record created) | |
| FR-APPR-003 | Manager rejects Submitted request | Must Have | §4 In Scope | Api, Application, Domain, Infrastructure | TC-APPR-P-003 (reject transitions to Rejected, Approval record created) | |
| FR-APPR-004 | Manager includes optional comment with decision | Must Have | §4 In Scope | Api, Application, Infrastructure | TC-APPR-P-004 (comment persisted in Approval record), TC-APPR-P-005 (null comment accepted) | |
| FR-APPR-005 | Exactly one Approval record created per decision | Must Have | §5 Business Constraints | Domain, Application, Infrastructure | TC-APPR-P-006 (one Approval record per decision), TC-APPR-N-002 (duplicate approval rejected) | BR-APPR-001 |
| FR-APPR-006 | Approver identity derived exclusively from authenticated session | Must Have | §5 Technical Constraints | Api, Application | TC-APPR-N-003 (forged approver ID in body ignored; session identity used) | BR-USER-002 (identity derivation); NFR-SEC-002; BR-APPR-002 |
| FR-APPR-007 | Only Submitted requests can be approved or rejected | Must Have | §5 Business Constraints | Domain, Application | TC-APPR-N-004 (approve Draft rejected), TC-APPR-N-005 (approve Approved rejected), TC-APPR-N-006 (approve Cancelled rejected) | BR-MGR-001 |
| FR-APPR-008 | Manager can only decide on requests from assigned employees | Must Have | §5 Business Constraints | Application, Infrastructure | TC-APPR-N-007 (non-assigned manager approve/reject returns 403) | BR-MGR-003; depends on FR-AUTH-010 / BR-DATA-002 |
| FR-APPR-009 | Manager cannot approve or reject own request | Must Have | §5 Business Constraints | Application, Domain | TC-APPR-N-008 (self-approval returns error) | BR-MGR-004 |

---

## 3. Non-Functional Requirements Traceability

| NFR ID | Description | Category | Priority | Architecture Layer(s) | Planned Test / Verification | Verification Method |
|--------|-------------|----------|----------|-----------------------|-----------------------------|--------------------|
| NFR-PERF-001 | Reviewer responsiveness — all API calls < 2 s, pages < 3 s (single local user) | Performance | Medium | Api, Infrastructure, Web | TC-PERF-001 (manual timing walkthrough of acceptance script) | Manual walkthrough; no load test tool required |
| NFR-SEC-001 | Passwords stored as cryptographic hashes (BCrypt or equivalent) | Security | Critical | Infrastructure / Identity | TC-SEC-001 (post-registration DB inspection confirms hash) | Code review + DB inspection |
| NFR-SEC-002 | User identity always derived from server-side session or validated token | Security | Critical | Api (auth middleware), Application | TC-SEC-002 (forged userId in body produces 401/403) | Code review + manual penetration test |
| NFR-SEC-003 | Role-based authorization enforced by API for all ownership and role checks | Security | Critical | Application, Domain | TC-SEC-003 (each authorization rule checked via direct API call) | Code review + manual API testing |
| NFR-SEC-004 | SQLite database file excluded from source control; no real credentials committed | Security | Critical | Infrastructure / DevOps | TC-SEC-004 (repo inspection confirms .gitignore and absence of committed DB) | Repository inspection |
| NFR-SEC-005 | Authenticated session maintained across requests; logout invalidates session | Security | Critical | Api (auth middleware), Application | TC-SEC-005 (post-logout request rejected) | Manual test |
| NFR-AVAIL-001 | Cold start succeeds without manual intervention; application stable for review session | Availability | High | Api, Infrastructure, Web | TC-AVAIL-001 (end-to-end acceptance walkthrough from clean state) | Acceptance walkthrough |
| NFR-AVAIL-002 | Blocking defects resolved before final acceptance | Availability | Medium | Cross-cutting | TC-AVAIL-002 (all acceptance workflow steps complete without error) | Live acceptance session |
| NFR-USE-001 | UI does not display invalid actions for current request state | Usability | High | Web | TC-USE-001 (manual state-by-state UI review for both roles) | Manual UI review |
| NFR-USE-002 | All forms use clear labels; validation errors are human-readable | Usability | High | Web | TC-USE-002 (reviewer confirms form usability without guidance) | Manual review during acceptance |
| NFR-USE-003 | Application usable via keyboard navigation; functional at 150% zoom | Usability | Medium | Web | TC-USE-003 (keyboard-only navigation walkthrough) | Manual check |
| NFR-REL-001 | All defined state transitions enforced; invalid transitions rejected | Reliability | Critical | Domain, Application | TC-REL-001 (invalid transitions produce 400/422 via direct API) | Code review + manual API testing |
| NFR-REL-002 | Date validation rules enforced at API layer for create and edit | Reliability | Critical | Domain, Application | TC-REL-002 (past start date rejected, end before start rejected) | API testing |
| NFR-REL-003 | Every approval/rejection creates exactly one Approval record with server-derived manager identity | Reliability | Critical | Domain, Application, Infrastructure | TC-REL-003 (post-decision DB inspection confirms single Approval record) | Code review + DB inspection |
| NFR-REL-004 | All session data persists after API restart if DB file not deleted | Reliability | Critical | Infrastructure (EF Core) | TC-REL-004 (restart without deleting DB; verify data present) | Manual test |
| NFR-MAINT-001 | Reduced Onion Architecture layer separation enforced | Maintainability | High | All layers | TC-MAINT-001 (code review confirms project reference rules) | Code review |
| NFR-MAINT-002 | No mediator, generic repository, event bus, or CQRS framework introduced | Maintainability | High | All layers | TC-MAINT-002 (NuGet package reference audit) | Package audit |
| NFR-MAINT-003 | Domain business rules and application authorization checks have automated test coverage ≥ 80% / ≥ 70% | Maintainability | Medium | Domain, Application | TC-MAINT-003 (dotnet test with coverlet; coverage report reviewed) | dotnet test + coverage |
| NFR-MAINT-004 | README enables clean-machine reviewer to start application from source code | Maintainability | Medium | Infrastructure / DevOps | TC-MAINT-004 (dry run by team member not involved in writing README) | Dry run |
| NFR-COMPAT-001 | Application runs locally without Docker, cloud, or external database server | Compatibility | High | Infrastructure / DevOps | TC-COMPAT-001 (acceptance run on reviewer machine with documented prerequisites only) | Acceptance environment test |
| NFR-COMPAT-002 | Frontend functions on last 2 releases of Chrome, Edge, Firefox, Safari | Compatibility | Medium | Web | TC-COMPAT-002 (acceptance walkthrough on at least one supported browser) | Manual verification |
| NFR-COMP-001 | MVP data privacy acknowledgment; no formal regulatory framework required | Compliance | Medium (deferred) | Infrastructure / Data | Sponsor acknowledgment recorded in SI-001 §5 Assumptions | Sponsor acknowledgment |
| NFR-COMP-002 | No formal audit logging beyond Approval record required for MVP | Compliance | Medium (deferred) | Domain, Infrastructure | TC-COMP-001 (post-decision DB inspection confirms Approval record fields) | DB inspection |

---

## 4. Business Rules Traceability

This section uses the authoritative Business Rules Catalog (BR-001 v1.2) IDs as the primary key. The cross-reference to FRS-001 §5 IDs (BR-001 through BR-013) is provided for mapping continuity. All 21 rules from the catalog are represented; no rules from outside the catalog are introduced.

> **Note on BR-OWNER-### and BR-DATA-003 identifiers:** These identifiers do not exist in BR-001 v1.2. Ownership of edit, submit, and cancel operations is captured under BR-REQ-004 in the authoritative catalog. Prior versions of this RTM that cited BR-OWNER-001, BR-OWNER-002, BR-OWNER-003, or BR-DATA-003 have been corrected; the associated content is now correctly attributed to their respective catalog entries. The concept that all business rules are enforced by the API rather than only by the UI is supported implicitly by NFR-SEC-003; it is not a standalone business rule with its own catalog ID.

| Catalog Rule ID | FRS-001 Cross-Ref | Business Rule Summary | Implementing FRs | NFR Support | Planned Test Coverage | Status |
|-----------------|-------------------|-----------------------|------------------|-------------|-----------------------|--------|
| BR-USER-001 | — | Users must register with a unique email address; duplicate registration is rejected | FR-AUTH-001 | NFR-REL-002 | TC-AUTH-N-001 | Specified |
| BR-USER-002 | BR-010 (partial) | The API always derives the current user identity and the responsible approver from the authenticated session; the frontend must never send a trusted employee or approver identifier for business decisions | FR-AUTH-006, FR-APPR-006 | NFR-SEC-002 | TC-SEC-002, TC-APPR-N-003 | Specified |
| BR-USER-003 | Implicitly covered across role-based FRs (FR-EMP-*, FR-MGR-*) | Two roles exist in the system: Employee and Manager. Actions are distinct for role-specific operations. A Manager acting as owner of their own absence requests has access to the same request management actions as an Employee (create, edit, submit, cancel own requests), subject to the self-approval prohibition in BR-MGR-004 | FR-MGR-REQ-001 | NFR-SEC-003 | TC-MGR-P-001, TC-MGR-N-001 | Specified |
| BR-MGR-001 | BR-005 | Only Submitted requests can be approved or rejected | FR-APPR-007 | NFR-REL-001 | TC-APPR-N-004, TC-APPR-N-005, TC-APPR-N-006 | Specified |
| BR-MGR-002 | BR-006 | Only users registered with the Manager role may approve or reject absence requests; Employees are not permitted to approve or reject any request, including their own | FR-APPR-002, FR-APPR-003 | NFR-SEC-003 | TC-APPR-N-009 (Employee role attempts approve/reject rejected) | Specified |
| BR-MGR-003 | BR-006 | Only the Manager assigned to the employee who owns the request can approve or reject that request | FR-APPR-008 | NFR-SEC-003 | TC-APPR-N-007 | Specified; depends on BR-DATA-002 assignment model |
| BR-MGR-004 | BR-007 | A Manager cannot approve or reject their own request | FR-APPR-009, FR-MGR-REQ-002 | NFR-SEC-003 | TC-APPR-N-008, TC-MGR-N-002 | Specified |
| BR-REQ-001 | BR-001 | The end date of a request cannot be earlier than the start date | FR-EMP-007, FR-MGR-REQ-001 | NFR-REL-002 | TC-EMP-N-008, TC-MGR-N-001 | Specified |
| BR-REQ-002 | BR-002 | The start date of a request cannot be in the past | FR-EMP-008, FR-MGR-REQ-001 | NFR-REL-002 | TC-EMP-N-009, TC-MGR-N-001 | Specified |
| BR-REQ-003 | BR-003 | Only Draft requests can be edited | FR-EMP-002, FR-MGR-REQ-001 | NFR-REL-001 | TC-EMP-N-002, TC-MGR-N-001 | Specified |
| BR-REQ-004 | BR-004 | Only the request owner can edit, submit, or cancel their own request | FR-EMP-002, FR-EMP-003, FR-EMP-004, FR-MGR-REQ-001 | NFR-SEC-003 | TC-EMP-N-010 (non-owner edit rejected), TC-EMP-N-003 (non-owner submit), TC-EMP-N-011 (non-owner cancel rejected) | Specified |
| BR-REQ-005 | BR-004 (partial) | Cancellation is valid from Draft or Submitted state only; Approved, Rejected, and Cancelled states cannot be cancelled | FR-EMP-004, FR-MGR-REQ-001 | NFR-REL-001 | TC-EMP-P-004, TC-EMP-P-008, TC-EMP-N-004, TC-MGR-N-001 | Specified |
| BR-REQ-006 | Covered under state machine in FRS §3 | Submitting a request moves it from Draft to Submitted, making it visible to the assigned manager for review. All date validations (BR-REQ-001, BR-REQ-002) and the reason field validation (BR-FIELD-001) are re-evaluated at submission time | FR-EMP-003, FR-MGR-REQ-001 | NFR-REL-002 | TC-EMP-P-003, TC-EMP-N-003 | Specified |
| BR-STATE-001 | BR-009 | Approved, Rejected, and Cancelled are final states — no further state transitions are permitted from these states | FR-EMP-005, FR-APPR-007 | NFR-REL-001 | TC-EMP-N-006, TC-EMP-N-007, TC-APPR-N-005, TC-APPR-N-006 | Specified |
| BR-SEC-001 | BR-012 | Passwords must never be stored in plain text; every password must be stored exclusively as a cryptographic hash (bcrypt, Argon2, or PBKDF2) | FR-AUTH-002 | NFR-SEC-001 | TC-AUTH-P-002, TC-AUTH-N-003, TC-SEC-001 | Specified |
| BR-SEC-002 | SI-001 §5 (Technical Constraints) | The SQLite database file must not be committed to source control with real passwords and must not be publicly accessible; seeded accounts use controlled, non-real credentials | — | NFR-SEC-004 | TC-SEC-004 | Specified |
| BR-APPR-001 | BR-008 (partial) | Each approval or rejection decision creates exactly one Approval record linking the request, the authenticated manager, the decision outcome, the decision date, and an optional comment | FR-APPR-005 | NFR-REL-003 | TC-APPR-P-006, TC-APPR-N-002, TC-REL-003 | Specified |
| BR-APPR-002 | BR-010 (identity sourcing clause) | The decided_by field of the Approval record must always be populated with the identity of the authenticated manager from the server-side session; the frontend must never supply or influence this field | FR-APPR-006 | NFR-SEC-002 | TC-APPR-N-003 | Specified |
| BR-FIELD-001 | — | The reason field is required when creating or editing an absence request | FR-EMP-001, FR-EMP-002, FR-MGR-REQ-001 | NFR-REL-002 | TC-EMP-N-001 (missing reason rejected) | Specified |
| BR-DATA-001 | BR-011 | The absence type catalog is seeded at startup and is not user-maintainable | FR-AUTH-008 | — | TC-AUTH-P-007 | Specified |
| BR-DATA-002 | BR-013 | Each Employee record stores a single assigned Manager reference (one employee → one manager for the MVP), established via seed data or controlled setup — not through a self-service or administrative UI | FR-AUTH-010, FR-APPR-001, FR-APPR-008 | NFR-SEC-003 | TC-AUTH-P-009, TC-AUTH-N-009, TC-APPR-N-007 | Specified; pending James Parker confirmation per SI-001 §6 |

---

## 5. Use Case Traceability

| UC ID | Use Case | Implementing FRs | Business Rules | Planned Test Coverage |
|-------|----------|------------------|----------------|-----------------------|
| UC-001 | Employee Registers an Account | FR-AUTH-001, FR-AUTH-002 | BR-USER-001, BR-SEC-001, BR-USER-002 | TC-AUTH-P-001, TC-AUTH-N-001, TC-AUTH-N-002, TC-AUTH-P-002 |
| UC-002 | User Logs In | FR-AUTH-003, FR-AUTH-005, FR-AUTH-006 | BR-USER-002 | TC-AUTH-P-003, TC-AUTH-N-004, TC-AUTH-N-005, TC-AUTH-P-005 |
| UC-003 | Employee Creates a Draft Request | FR-EMP-001, FR-EMP-007, FR-EMP-008 | BR-REQ-001, BR-REQ-002, BR-FIELD-001 | TC-EMP-P-001, TC-EMP-N-001, TC-EMP-N-008, TC-EMP-N-009 |
| UC-004 | Employee Edits a Draft Request | FR-EMP-002, FR-EMP-007, FR-EMP-008 | BR-REQ-001, BR-REQ-002, BR-REQ-003, BR-REQ-004 | TC-EMP-P-002, TC-EMP-N-002, TC-EMP-N-010 |
| UC-005 | Employee Submits a Draft Request | FR-EMP-003 | BR-REQ-003, BR-REQ-004, BR-REQ-006 | TC-EMP-P-003, TC-EMP-N-003 |
| UC-006 | Employee Cancels a Request | FR-EMP-004 | BR-REQ-004, BR-REQ-005, BR-STATE-001 | TC-EMP-P-004, TC-EMP-P-008, TC-EMP-N-004, TC-EMP-N-011 |
| UC-007 | Employee Views Request List and Final Decision | FR-EMP-006 | BR-USER-002 | TC-EMP-P-005, TC-EMP-P-006 |
| UC-008 | Manager Reviews Submitted Requests | FR-APPR-001 | BR-MGR-002, BR-MGR-003, BR-DATA-002 | TC-APPR-P-001, TC-APPR-N-001 |
| UC-009 | Manager Approves a Submitted Request | FR-APPR-002, FR-APPR-004, FR-APPR-005, FR-APPR-006, FR-APPR-007, FR-APPR-008, FR-APPR-009 | BR-MGR-001, BR-MGR-003, BR-MGR-004, BR-APPR-001, BR-USER-002, BR-APPR-002 | TC-APPR-P-002, TC-APPR-P-004, TC-APPR-P-006, TC-APPR-N-003, TC-APPR-N-004, TC-APPR-N-007, TC-APPR-N-008 |
| UC-010 | Manager Rejects a Submitted Request | FR-APPR-003, FR-APPR-004, FR-APPR-005, FR-APPR-006, FR-APPR-007, FR-APPR-008, FR-APPR-009 | BR-MGR-001, BR-MGR-003, BR-MGR-004, BR-APPR-001, BR-USER-002, BR-APPR-002 | TC-APPR-P-003, TC-APPR-P-005, TC-APPR-P-006, TC-APPR-N-003, TC-APPR-N-007, TC-APPR-N-008 |
| UC-011 | Manager Submits Their Own Absence Request | FR-MGR-REQ-001 | BR-REQ-001, BR-REQ-002, BR-REQ-003, BR-REQ-004, BR-REQ-006 | TC-MGR-P-001, TC-MGR-N-001 |
| UC-012 | System Blocks Manager from Self-Approving | FR-MGR-REQ-002, FR-APPR-009 | BR-MGR-004 | TC-MGR-N-002, TC-APPR-N-008 |
| UC-013 | System Establishes Employee-Manager Assignment (Seed/Controlled Setup) | FR-AUTH-010 | BR-DATA-002 | TC-AUTH-P-009, TC-AUTH-N-009 |

---

## 6. State Transition Traceability

The VacaFlow request lifecycle is a central correctness concern. The following tables trace each valid and invalid state transition to its implementing requirements, business rules, and test coverage.

### 6.1 Valid Transitions

| Transition | Actor | Implementing FR | Business Rules | Planned Test |
|------------|-------|-----------------|----------------|--------------|
| Draft → Submitted | Request owner (Employee or Manager) | FR-EMP-003, FR-MGR-REQ-001 | BR-REQ-003, BR-REQ-004, BR-REQ-006 | TC-EMP-P-003 |
| Draft → Cancelled | Request owner (Employee or Manager) | FR-EMP-004, FR-MGR-REQ-001 | BR-REQ-004, BR-REQ-005 | TC-EMP-P-004 |
| Submitted → Cancelled | Request owner (Employee or Manager) | FR-EMP-004, FR-MGR-REQ-001 | BR-REQ-004, BR-REQ-005 | TC-EMP-P-008 |
| Submitted → Approved | Assigned Manager (not the owner) | FR-APPR-002 | BR-MGR-001, BR-MGR-003, BR-MGR-004, BR-APPR-001 | TC-APPR-P-002 |
| Submitted → Rejected | Assigned Manager (not the owner) | FR-APPR-003 | BR-MGR-001, BR-MGR-003, BR-MGR-004, BR-APPR-001 | TC-APPR-P-003 |

### 6.2 Invalid Transitions (Must Be Rejected)

| Attempted Transition | Expected Response | Implementing FR | Business Rules | Planned Test |
|----------------------|-------------------|-----------------|----------------|--------------|
| Submitted → Submitted (re-submit) | 400 / 422 | FR-EMP-003 | BR-REQ-003, BR-STATE-001 | TC-EMP-N-003 |
| Submitted → Edit by employee | 400 / 422 | FR-EMP-002 | BR-REQ-003 | TC-EMP-N-005 |
| Approved → any state | 400 / 422 | FR-EMP-005, FR-APPR-007 | BR-STATE-001 | TC-EMP-N-006, TC-APPR-N-005 |
| Rejected → any state | 400 / 422 | FR-EMP-005, FR-APPR-007 | BR-STATE-001 | TC-EMP-N-007, TC-APPR-N-005 |
| Cancelled → any state | 400 / 422 | FR-EMP-005, FR-APPR-007 | BR-STATE-001 | TC-EMP-N-007, TC-APPR-N-006 |
| Draft → Approved (skip submit) | 400 / 422 | FR-APPR-007 | BR-MGR-001 | TC-APPR-N-004 |
| Draft → Rejected (skip submit) | 400 / 422 | FR-APPR-007 | BR-MGR-001 | TC-APPR-N-004 |
| Non-owner cancels any request | 400 / 403 | FR-EMP-004 | BR-REQ-004 | TC-EMP-N-011 |
| Manager approves/rejects non-assigned employee's request | 403 | FR-APPR-008 | BR-MGR-003 | TC-APPR-N-007 |
| Manager approves/rejects own request | 403 | FR-APPR-009, FR-MGR-REQ-002 | BR-MGR-004 | TC-APPR-N-008 |

---

## 7. Forward Traceability

### Authentication Domain

```
FR-AUTH-001: User Registration
├── Architecture: Api endpoint (POST /auth/register)
│   ├── Component: Application — RegisterUserUseCase
│   ├── Component: Infrastructure — UserRepository, PasswordHasher (BCrypt)
│   └── Component: Domain — User entity, Role enum
├── Test Cases:
│   ├── TC-AUTH-P-001: Successful registration with valid inputs
│   ├── TC-AUTH-N-001: Duplicate email rejected
│   └── TC-AUTH-N-002: Missing required field rejected
└── Supports Business Rules: BR-USER-001, BR-USER-002
```

```
FR-AUTH-002: Password Hashing (Critical Security Requirement)
├── Architecture: Infrastructure — PasswordHasher
│   └── Component: BCrypt (or PBKDF2 / Argon2)
├── Test Cases:
│   ├── TC-AUTH-P-002: Post-registration DB inspection confirms hash
│   └── TC-AUTH-N-003: No plain-text password in DB
└── Supports NFR: NFR-SEC-001
    Supports Business Rules: BR-SEC-001
```

```
FR-AUTH-010: Employee-Manager Assignment (Seed)
├── Architecture: Infrastructure — DatabaseSeeder, Employee entity (ManagerId FK)
│   └── Component: Domain — Employee entity with single ManagerId
├── Test Cases:
│   ├── TC-AUTH-P-009: Employee record has manager reference post-seed
│   └── TC-AUTH-N-009: Non-assigned manager cannot approve employee's request
└── Supports Business Rules: BR-DATA-002, BR-MGR-003
```

### Request Lifecycle Domain

```
FR-EMP-003: Submit Draft Request
├── Architecture: Api endpoint (POST /requests/{id}/submit)
│   ├── Component: Application — SubmitRequestUseCase
│   └── Component: Domain — RequestStateMachine (Draft → Submitted)
├── Test Cases:
│   ├── TC-EMP-P-003: Submit transitions to Submitted state
│   └── TC-EMP-N-003: Submit on non-Draft returns error
└── Supports Business Rules: BR-REQ-003, BR-REQ-004, BR-REQ-006
```

```
FR-EMP-004: Cancel Draft or Submitted Request
├── Architecture: Api endpoint (POST /requests/{id}/cancel)
│   ├── Component: Application — CancelRequestUseCase
│   └── Component: Domain — RequestStateMachine
│       ├── Valid: Draft → Cancelled
│       └── Valid: Submitted → Cancelled
├── Test Cases:
│   ├── TC-EMP-P-004: Cancel Draft → Cancelled state
│   ├── TC-EMP-P-008: Cancel Submitted → Cancelled state
│   ├── TC-EMP-N-004: Cancel from Approved/Rejected/Cancelled returns error
│   └── TC-EMP-N-011: Non-owner cancel attempt returns 403
└── Supports Business Rules: BR-REQ-004, BR-REQ-005, BR-STATE-001
```

```
FR-APPR-005: Exactly One Approval Record Per Decision
├── Architecture: Api endpoint (POST /requests/{id}/approve or /reject)
│   ├── Component: Application — ApproveRequestUseCase / RejectRequestUseCase
│   ├── Component: Domain — ApprovalRecord entity
│   └── Component: Infrastructure — ApprovalRepository
├── Test Cases:
│   ├── TC-APPR-P-006: One Approval record in DB after decision
│   └── TC-APPR-N-002: Duplicate approval rejected
└── Supports Business Rules: BR-APPR-001
    Supports NFR: NFR-REL-003
```

```
FR-APPR-006: Approver Identity From Authenticated Session
├── Architecture: Api (auth middleware), Application — use case authorization
│   └── Component: Session identity reader; no client-supplied approver field accepted
├── Test Cases:
│   └── TC-APPR-N-003: Forged approver ID in body ignored; session identity used
└── Supports Business Rules: BR-USER-002, BR-APPR-002
    Supports NFR: NFR-SEC-002
```

```
FR-EMP-007 / FR-EMP-008: Date Validation
├── Architecture: Domain — AbsenceRequestValidator (date rules)
│   ├── Component: Application — CreateRequestUseCase, EditRequestUseCase
│   └── Component: Api — validation middleware / endpoint handler
├── Test Cases:
│   ├── TC-EMP-N-008: End date before start date rejected
│   ├── TC-EMP-P-007: Same-day request accepted
│   └── TC-EMP-N-009: Past start date rejected
└── Supports Business Rules: BR-REQ-001, BR-REQ-002
    Supports NFR: NFR-REL-002
```

### Security Domain

```
NFR-SEC-003: Role-Based Authorization Enforcement
├── Architecture: Application — authorization guards in each use case
│   ├── Component: OwnershipGuard (FR-EMP, FR-MGR-REQ via BR-REQ-004)
│   ├── Component: ManagerAssignmentGuard (FR-APPR-008 via BR-MGR-003)
│   └── Component: SelfApprovalGuard (FR-APPR-009 via BR-MGR-004)
├── Test Cases:
│   ├── TC-SEC-003: All authorization rules verified by direct API call
│   ├── TC-APPR-N-007: Non-assigned manager returns 403
│   └── TC-APPR-N-008: Self-approval returns 403
└── Supports Business Rules: BR-REQ-004, BR-MGR-002, BR-MGR-003, BR-MGR-004
```

---

## 8. Backward Traceability

### From Test Areas to Requirements

| Test Area | Verifies | Requirement(s) | Business Rule(s) |
|-----------|----------|----------------|-----------------|
| TC-AUTH-P-001 | Valid registration flow | FR-AUTH-001 | BR-USER-001, BR-USER-002 |
| TC-AUTH-P-002 | Hash stored post-registration | FR-AUTH-002 | BR-SEC-001 |
| TC-AUTH-P-003 | Valid login | FR-AUTH-003 | BR-USER-002 |
| TC-AUTH-P-008 | Cold start: schema migrated and seeded | FR-AUTH-009 | BR-DATA-001 |
| TC-AUTH-P-009 | Employee record has manager reference post-seed | FR-AUTH-010 | BR-DATA-002 |
| TC-AUTH-N-001 | Duplicate email rejected | FR-AUTH-001 | BR-USER-001 |
| TC-AUTH-N-008 | Unauthenticated business request rejected | FR-AUTH-006 | BR-USER-002 |
| TC-AUTH-N-009 | Non-assigned manager cannot approve employee's request | FR-AUTH-010, FR-APPR-008 | BR-DATA-002, BR-MGR-003 |
| TC-EMP-P-003 | Submit Draft → Submitted | FR-EMP-003 | BR-REQ-003, BR-REQ-004, BR-REQ-006 |
| TC-EMP-P-004 | Cancel Draft → Cancelled | FR-EMP-004 | BR-REQ-004, BR-REQ-005 |
| TC-EMP-P-008 | Cancel Submitted → Cancelled | FR-EMP-004 | BR-REQ-004, BR-REQ-005 |
| TC-EMP-N-002 | Edit non-Draft rejected | FR-EMP-002 | BR-REQ-003 |
| TC-EMP-N-008 | End before start rejected | FR-EMP-007 | BR-REQ-001 |
| TC-EMP-N-009 | Past start date rejected | FR-EMP-008 | BR-REQ-002 |
| TC-EMP-N-010 | Non-owner edit rejected | FR-EMP-002 | BR-REQ-004 |
| TC-EMP-N-011 | Non-owner cancel rejected | FR-EMP-004 | BR-REQ-004 |
| TC-APPR-P-002 | Approve → Approved + Approval record | FR-APPR-002, FR-APPR-005 | BR-MGR-001, BR-MGR-003, BR-APPR-001 |
| TC-APPR-N-003 | Forged approver ID ignored | FR-APPR-006 | BR-USER-002, BR-APPR-002 |
| TC-APPR-N-007 | Non-assigned manager approve/reject rejected | FR-APPR-008 | BR-MGR-003 |
| TC-APPR-N-008 | Self-approval rejected | FR-APPR-009, FR-MGR-REQ-002 | BR-MGR-004 |
| TC-APPR-N-009 | Employee role attempts approve/reject rejected | FR-APPR-002, FR-APPR-003 | BR-MGR-002 |
| TC-SEC-001 | No plain-text password in DB | FR-AUTH-002 | BR-SEC-001 |
| TC-SEC-002 | Forged userId in body produces 403 | FR-AUTH-006, FR-APPR-006 | BR-USER-002 |
| TC-SEC-003 | All role rules enforced at API | FR-AUTH-006, FR-APPR-007, FR-APPR-008, FR-APPR-009 | BR-MGR-001, BR-MGR-003, BR-MGR-004 |
| TC-SEC-004 | DB file not committed; .gitignore present | — | BR-SEC-002 |
| TC-REL-001 | Invalid state transitions produce 400/422 | FR-EMP-005, FR-APPR-007 | BR-MGR-001, BR-STATE-001 |
| TC-AVAIL-001 | Full acceptance walkthrough from cold start | NFR-AVAIL-001, FR-AUTH-009 | — |
| TC-MAINT-003 | Domain/Application test coverage ≥ 70–80% | NFR-MAINT-003 | — |

---

## 9. Coverage Analysis

### 9.1 Functional Requirements Coverage Summary

| Module | Total FRs | With Component Allocation | With Planned Test Coverage | Coverage |
|--------|-----------|--------------------------|---------------------------|----------|
| Authentication (FR-AUTH) | 10 | 10 | 10 | 100% |
| Employee Requests (FR-EMP) | 8 | 8 | 8 | 100% |
| Manager as Requester (FR-MGR-REQ) | 2 | 2 | 2 | 100% |
| Manager Approval (FR-APPR) | 9 | 9 | 9 | 100% |
| **Total** | **29** | **29** | **29** | **100%** |

> All 29 functional requirements have at least one planned test case. Formal test case IDs will be assigned when the `/quality/test-cases` skill is executed.

### 9.2 Non-Functional Requirements Coverage Summary

| Category | Total NFRs | With Architecture Allocation | With Verification Method | Coverage |
|----------|------------|------------------------------|-------------------------|----------|
| Performance | 1 | 1 | 1 | 100% |
| Security | 5 | 5 | 5 | 100% |
| Availability | 2 | 2 | 2 | 100% |
| Usability | 3 | 3 | 3 | 100% |
| Reliability | 4 | 4 | 4 | 100% |
| Maintainability | 4 | 4 | 4 | 100% |
| Compatibility | 2 | 2 | 2 | 100% |
| Compliance | 2 | 2 | 2 | 100% |
| **Total** | **23** | **23** | **23** | **100%** |

### 9.3 Business Rules Coverage Summary

| Category | Total Rules | With Implementing FR | With Planned Test | Coverage |
|----------|-------------|---------------------|-------------------|----------|
| User Rules (BR-USER-###) | 3 | 3 | 3 | 100% |
| Manager Rules (BR-MGR-###) | 4 | 4 | 4 | 100% |
| Request Rules (BR-REQ-###) | 6 | 6 | 6 | 100% |
| State Rules (BR-STATE-###) | 1 | 1 | 1 | 100% |
| Security Rules (BR-SEC-###) | 2 | 2 | 2 | 100% |
| Approval Record Rules (BR-APPR-###) | 2 | 2 | 2 | 100% |
| Field Rules (BR-FIELD-###) | 1 | 1 | 1 | 100% |
| Data Rules (BR-DATA-###) | 2 | 2 | 2 | 100% |
| **Total** | **21** | **21** | **21** | **100%** |

### 9.4 Use Case Coverage Summary

| Module | Total UCs | With FR Mapping | With Planned Test | Coverage |
|--------|-----------|-----------------|-------------------|----------|
| Authentication | 2 | 2 | 2 | 100% |
| Employee Requests | 5 | 5 | 5 | 100% |
| Manager Approval | 4 | 4 | 4 | 100% |
| System / Admin | 2 | 2 | 2 | 100% |
| **Total** | **13** | **13** | **13** | **100%** |

### 9.5 Gaps Identified

| Gap Type | Count | Items | Resolution |
|----------|-------|-------|------------|
| No SAD Reference | 86 | All requirements | SAD not yet produced; component allocations are layer-level and will be refined following the architecture phase |
| No Formal Test Case ID | 86 | All requirements | Test case IDs (TC-###) are planned references; formal IDs will be assigned when the `/quality/test-cases` skill is executed |
| Pending Business Confirmation | 3 | BR-DATA-002, FR-AUTH-010, FR-APPR-001 / FR-APPR-008 (depend on BR-DATA-002) | James Parker (Sponsor) must confirm one-to-one Employee-Manager assignment model; open per SI-001 §6 |

---

## 10. Change Impact Analysis

### 10.1 Impact Matrix

| If This Changes | Check and Update |
|-----------------|-----------------|
| FR-AUTH-010 / BR-DATA-002 (Manager assignment model) | FR-APPR-001, FR-APPR-008, UC-008, UC-009, UC-010, UC-013; NFR-SEC-003 authorization rules; TC-APPR-N-007, TC-AUTH-N-009 |
| State transition rules (BR-REQ-003, BR-MGR-001, BR-REQ-005, BR-REQ-006, BR-STATE-001) | FR-EMP-002–005, FR-EMP-003, FR-APPR-007; Section 6 (state transition tables); all TC-*-N-* invalid transition tests |
| Session identity enforcement (BR-USER-002, BR-APPR-002, NFR-SEC-002) | FR-AUTH-006, FR-APPR-006; TC-SEC-002, TC-APPR-N-003; Api auth middleware component |
| Date validation rules (BR-REQ-001, BR-REQ-002) | FR-EMP-007, FR-EMP-008, FR-MGR-REQ-001; TC-EMP-N-008, TC-EMP-N-009, TC-MGR-N-001 |
| Approval record requirements (BR-APPR-001, NFR-REL-003) | FR-APPR-005; TC-APPR-P-006, TC-APPR-N-002; Domain ApprovalRecord entity; Infrastructure ApprovalRepository |
| Architecture stack (NFR-MAINT-001, NFR-MAINT-002) | All layer allocations throughout RTM; NFR-MAINT-003 test coverage targets |
| Seeded absence types or manager account (FR-AUTH-007, FR-AUTH-008) | TC-AUTH-P-006, TC-AUTH-P-007, TC-AVAIL-001 |
| Ownership-based authorization rules (BR-REQ-004) | FR-EMP-002, FR-EMP-003, FR-EMP-004, FR-MGR-REQ-001; TC-EMP-N-010, TC-EMP-N-011 |
| Reason field rule (BR-FIELD-001) | FR-EMP-001, FR-EMP-002, FR-MGR-REQ-001; TC-EMP-N-001 |
| Self-approval prohibition (BR-MGR-004) | FR-APPR-009, FR-MGR-REQ-002; TC-APPR-N-008, TC-MGR-N-002 |
| Password hashing requirement (BR-SEC-001) | FR-AUTH-002; TC-AUTH-P-002, TC-AUTH-N-003, TC-SEC-001; Infrastructure PasswordHasher component |
| Database file protection (BR-SEC-002) | NFR-SEC-004; TC-SEC-004; repository .gitignore configuration |
| Role-based authorization enforcement (NFR-SEC-003) | All Application-layer authorization guards; TC-SEC-003; all TC-*-N-* series (direct API call tests) |

### 10.2 Open Change Items

| Item | Source | Impact | Owner | Status |
|------|--------|--------|-------|--------|
| Confirm one-to-one Employee-Manager assignment model (BR-DATA-002) | SI-001 §6 Critical Information Gap | FR-AUTH-010, FR-APPR-001, FR-APPR-008, UC-008–010, UC-013 | James Parker (Sponsor) | Open — pending confirmation |

---

## 11. Verification Matrix

### 11.1 Verification Methods Used in VacaFlow

| Method | Description | Applied To |
|--------|-------------|------------|
| Automated Unit Test | `dotnet test` run on Domain and Application layers | Business rule logic, state transitions, date validation, authorization checks |
| Integration Test | API endpoint tests exercising the full stack (Api + Application + Infrastructure) | Happy paths and primary error paths per endpoint group |
| Code Review | Manual review of project references, package list, and implementation against specifications | NFR-MAINT-001, NFR-MAINT-002, NFR-SEC-001, NFR-SEC-002, NFR-SEC-003, NFR-REL-003 |
| Database Inspection | Opening the SQLite file to confirm stored values | NFR-SEC-001 (hash), NFR-SEC-004 (no committed DB), NFR-REL-003 (Approval record), NFR-COMP-002 |
| Manual API Test | Direct API calls via Postman / curl without frontend | NFR-SEC-002 (forged ID), NFR-SEC-003 (authorization rules), NFR-REL-001 (invalid transitions) |
| Acceptance Walkthrough | End-to-end reviewer session per SI-001 §5 | NFR-AVAIL-001, NFR-AVAIL-002, NFR-PERF-001, NFR-USE-001, NFR-USE-002, NFR-COMPAT-001 |
| Repository Inspection | Git repository review | NFR-SEC-004 (`.gitignore`, no committed DB file) |
| Coverage Report | `dotnet test` + coverlet for line coverage | NFR-MAINT-003 |
| Dry Run | Clean-machine README validation | NFR-MAINT-004 |

### 11.2 Critical Verification Items (Acceptance-Gating)

The following items are explicitly listed as acceptance rejection conditions in SI-001 or NFR-001:

| Item | Verification Method | Target | Rejection Condition |
|------|--------------------|---------|--------------------|
| Password hashing (NFR-SEC-001, FR-AUTH-002, BR-SEC-001) | Code review + DB inspection | TC-SEC-001 | Plain-text password found in DB → hard rejection |
| API identity enforcement (NFR-SEC-002, BR-USER-002, BR-APPR-002) | Manual API test | TC-SEC-002 | Frontend-supplied identity accepted by API → hard rejection |
| Role-based authorization (NFR-SEC-003, BR-MGR-003, BR-MGR-004, BR-REQ-004) | Code review + manual API test | TC-SEC-003 | Any bypass produces unauthorized business action → hard rejection |
| State machine integrity (NFR-REL-001, BR-MGR-001, BR-STATE-001) | Code review + manual API test | TC-REL-001 | Invalid transition succeeds → rejection |
| End-to-end acceptance walkthrough (NFR-AVAIL-001) | Acceptance session | TC-AVAIL-001 | Any crash or unrecoverable error during workflow → blocking defect |

---

## 12. Status Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented and verified |
| ⏳ | In progress |
| 📋 | Specified — awaiting implementation |
| ⚠️ | At risk or pending external confirmation |
| ❌ | Blocked |
| 📊 | Monitoring required |

> All requirements in this RTM are currently at **📋 Specified** status. Implementation and test execution status will be updated as the project progresses through Architecture, Development, and Quality phases.

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-07-20 | David Valdez, Laura Hernandez (AI Assisted) | Initial RTM — covers 29 FRs, 23 NFRs, 13 business rules (FRS-001 §5 IDs), 13 use cases; bidirectional traceability to FRS-001 (v1.2) and NFR-001 (v3.0); reflected UC-006 Cancel scope as Draft only per sources feedback |
| 2.0 | 2026-07-20 | David Valdez, Laura Hernandez (AI Assisted) | Applied reviewer feedback: (1) Restored Cancel to Draft and Submitted scope per FRS-001 FR-EMP-004 and BR-REQ-005; added valid transition Submitted → Cancelled to §6.1; removed invalid-transition row for employee-cancelled-Submitted; restored UC-006 title; added TC-EMP-P-008; (2) Corrected Executive Summary total FRs from 27 to 29 and coverage total from 76 to 86; (3) Rewrote §4 using authoritative BR-001 v1.2 catalog IDs — 21 rules total; updated §9.3 coverage from 13 to 21 rules; (4) Corrected §1.3 Document References — all four predecessor documents show Approved status |
| 3.0 | 2026-07-21 | David Valdez, Laura Hernandez (AI Assisted) | Applied reviewer feedback: (1) Rewrote §4 Business Rules Traceability using exactly the 21 BR-001 v1.2 catalog IDs as primary keys — eliminated non-existent BR-OWNER-001, BR-OWNER-002, BR-OWNER-003 (content consolidated under BR-REQ-004) and non-existent BR-DATA-003 (API-enforcement concept supported implicitly by NFR-SEC-003, not a standalone business rule); added BR-MGR-002, BR-MGR-003, BR-APPR-001, BR-APPR-002 with correct content; corrected §9.3 coverage table categories to User Rules 3, Manager Rules 4, Request Rules 6, State Rules 1, Security Rules 2, Approval Record Rules 2, Field Rules 1, Data Rules 2 — total 21; (2) Propagated corrected BR IDs across §5–§11 |
| 4.0 | 2026-07-21 | David Valdez, Laura Hernandez (AI Assisted) | Applied reviewer audit feedback: (1) Confirmed all 21 BR-001 v1.2 catalog IDs against the authoritative catalog — no undeclared IDs remain; (2) Corrected §4 FRS-001 cross-reference for BR-SEC-001 from BR-010 (partial) to BR-012; (3) Added BR-SEC-001 to §10.1 Impact Matrix as an explicit entry; (4) Expanded §7 Forward Traceability to include date validation (FR-EMP-007/FR-EMP-008) and security authorization (NFR-SEC-003) nodes; (5) Verified §9.3 totals remain 21 across all 8 categories |
| 5.0 | 2026-07-21 | David Valdez, Laura Hernandez (AI Assisted) | Applied reviewer feedback (final ID reclassification): (1) Replaced BR-USER-003 row in §4 with correct content — dual-role behavior of Manager acting as requester (was incorrectly set to absence type catalog rule, which is properly BR-DATA-001); (2) Replaced BR-MGR-002 row in §4 with correct content — role restriction preventing Employees from approving or rejecting (was duplicating BR-MGR-003 manager-assignment content); (3) Replaced BR-REQ-006 row in §4 with correct content — Draft-to-Submitted transition and re-evaluation of validations (was incorrectly defined as Approval record creation, which is BR-APPR-001); (4) Replaced BR-SEC-001 row in §4 with correct content — password hashing rule (was incorrectly defined as general API enforcement principle); (5) Replaced BR-SEC-002 row in §4 with correct content — database file protection (was incorrectly defined as password hashing); (6) Replaced BR-APPR-002 row in §4 with correct content — decided_by field must be populated from authenticated session (was incorrectly defined as optional comment null-allowed rule, which is subsumed by BR-APPR-001); (7) Removed the false note "API enforcement as a general principle is captured under BR-SEC-001" from §4 header — that concept is supported by NFR-SEC-003 and has no catalog BR-* ID; (8) Removed BR-SEC-001 from the Security Domain forward traceability block title and Supports Business Rules line in §7 — that block covers role-based authorization (BR-MGR-002, BR-MGR-003, BR-MGR-004, BR-REQ-004), not password hashing; (9) Propagated all ID reclassifications across §5 (UC BR citations), §6 (state transition BR citations), §7 (forward traceability blocks), §8 (backward traceability table), §10.1 (impact matrix entries), and §11.2 (critical verification table); added FR-APPR-006 dedicated forward traceability block citing BR-USER-002 and BR-APPR-002; added TC-APPR-N-009 to §8 backward traceability; added BR-SEC-002 and database file protection row to §10.1 impact matrix; (10) Verified §9.3 totals remain 21 across all 8 categories — Security Rules = 2, Manager Rules = 4, no category at zero |

---
## Document Control

| Field | Value |
|-------|-------|
| Author | David Valdez, Laura Hernandez (AI Assisted) |
| Approval Authority | Solution Architect |
| Status | Draft |
| Signature | ⏳ Pending — awaiting approval |

*— End of document —*
