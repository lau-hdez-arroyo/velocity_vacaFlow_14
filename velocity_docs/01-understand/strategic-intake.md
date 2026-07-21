# Strategic Intake Document

**Project:** VacaFlow_14
**Document ID:** SI-001
**Stage:** 01 — Understand
**Author:** Laura Hernandez (AI Assisted)
**BSA/PO:** Laura Hernandez
**Sponsor:** James Parker
**Organization:** IGS Solutions
**Date:** 2026-07-18
**Status:** Draft

---

## 1. Project Context

IGS Solutions currently manages all employee vacation and absence requests through informal channels: email threads, Microsoft Teams chat messages, and spreadsheets. Employees submit requests by writing to their manager via email or chat; managers respond through those same channels. There is no centralized system of record for request status, approval ownership, or final decisions. No role separation is enforced, no audit trail exists, and the lifecycle of any given request — from submission through decision — is reconstructed from message history rather than from a structured record.

This informal process has produced measurable operational consequences. Managers spend excessive time confirming request status because there is no single source of truth employees can consult. Approval ownership is unenforceable because no record links a decision to the manager who made it. Most critically, a documented incident has already materialized: an employee acted on an informal chat reply as though it were a formal approval, while no decision was ever recorded — creating a discrepancy between the employee's understanding of the request state and the actual state. This incident demonstrates that the informal process is not merely inefficient; it produces incorrect real-world outcomes.

**Why now?**
Two specific, concurrent triggers make action necessary at this time. First, the informal-approval incident described above has already occurred and been documented — this is not a hypothetical risk but a confirmed operational failure. Second, the volume of informal requests has grown to the point where managers are absorbing a disproportionate productivity cost simply confirming request status through email and chat. The informal process has reached a breaking point: it cannot scale further without generating additional incidents of the same type.

---

## 2. Value Proposition

> **For** IGS Solutions employees and their managers, **who need** a reliable, traceable way to submit and decide on vacation and absence requests, **VacaFlow offers** a structured request lifecycle with enforced state transitions, authenticated decision recording, and a single source of truth for every request and its outcome, **unlike** the current combination of email, Microsoft Teams chat, and spreadsheets, which provide no formal record, no role enforcement, and no accountability trail.

### Value Comparison

| Dimension | VacaFlow (Proposed) | Email / Teams / Spreadsheets (Current) |
|-----------|---------------------|----------------------------------------|
| **Request state visibility** | Single queryable status per request (Draft → Submitted → Approved / Rejected / Cancelled), visible to employee and manager at any time | No defined states; status is inferred from message history and requires follow-up to confirm |
| **Approval accountability** | Every approval or rejection creates one Approval record with the authenticated manager recorded as responsible approver; unbypassable | No record links a decision to a specific manager; accountability is unenforceable |
| **Incident recurrence risk** | Eliminated by design: only a Submitted request can be approved, only by the assigned authenticated Manager, and the decision is always persisted | Confirmed incident already occurred; structural conditions for recurrence remain unchanged |
| **Manager time spent confirming status** | Managers see only Submitted requests awaiting their decision; employees see full request history without needing to follow up | Managers fielding repeated status inquiries via email and chat; no self-service alternative |
| **Audit trail** | Every decision stored with manager identity, decision date, and optional comment | No durable record; audit reconstruction requires searching message history |

---

## 3. Stakeholders

| Name / Role | Influence | What They Decide or Need to Answer |
|-------------|-----------|--------------------------------------|
| James Parker · Operations Manager / Project Sponsor | High | Confirms scope boundaries; provides functional sign-off; accepts the final MVP after live end-to-end demonstration; decides whether blocking defects found during review require remediation before acceptance |
| Emily Harrison · Functional Analyst | Medium | Elicits and documents requirements; manages scope decisions and delivery artifacts; responsible for keeping decision records current throughout the project **RISK:** If Emily is unavailable during the documentation or acceptance phase, artifact ownership becomes unclear — James Parker must designate a backup before kickoff |

---

## 4. Business Context & Scope

### Expected Value (Quantified)

**Measurable Benefits:**
- **Request status uncertainty → eliminated:** Current = employees and managers must search message history to confirm request state; Target = every request has a single queryable status (Draft, Submitted, Approved, Rejected, Cancelled) visible to both parties from the moment the MVP is in use
- **Approval ownership ambiguity → eliminated:** Current = no record links a decision to the manager who made it; Target = 100% of approval and rejection decisions produce one Approval record with the authenticated manager as responsible approver — enforced by the system, not by convention
- **Informal-approval incident recurrence → prevented:** Current = confirmed incident already occurred; structural conditions unchanged; Target = the system enforces that only a Submitted request can be approved, only by an authenticated Manager assigned to that employee, making the same failure mode technically impossible
- **Manager time spent confirming request status → reduced:** Current = managers handling repeated status inquiries through email and chat with no self-service alternative; Target = managers view only Submitted requests awaiting their decision; employees view full request history without follow-up required

**Qualitative Benefits:**
- IGS Solutions gains a demonstrable, repeatable end-to-end workflow for absence management that can serve as the foundation for future enhancements without being constrained by the current informal baseline
- The project validates a modern local technology stack (Next.js, ASP.NET Core Minimal API, SQLite, EF Core, Onion Architecture) as a viable delivery pattern for future internal tools at IGS Solutions

### Scope Boundaries

**In Scope:**
- Local application-managed registration and login (name, email, hashed password, role selection — Employee or Manager)
- Two application roles: Employee and Manager
- Four business entities: Employee, Absence Type (Vacation, Personal Leave, Sick Leave — seeded), Request, and Approval
- Full request lifecycle: Draft, Submitted, Approved, Rejected, Cancelled — with all valid state transitions enforced by the API
- Employee actions: register, log in, log out, view own requests, create a Draft request, edit a Draft request, submit a Draft request, cancel a Draft or Submitted request, view final decision
- Manager actions: log in, view Submitted requests assigned to them, approve or reject a Submitted request with an optional comment
- Business rules enforced server-side: end date cannot precede start date; start date cannot be in the past; only Draft requests can be edited; only the request owner can edit, submit, or cancel; only the assigned Manager can approve or reject; a manager cannot approve or reject their own request; every approval or rejection creates one Approval record with the authenticated manager as responsible approver
- Authentication: register, login, logout, current-user endpoints; API derives identity from the authenticated session — the frontend never sends a trusted employee or approver identifier
- Web screens: register, login, employee request list, request creation and edit form, manager review list with approve and reject actions
- SQLite database with seeded absence types and at least one seeded manager account; automatic migration on startup
- Stack: ASP.NET Core Minimal API, Next.js/React, Entity Framework Core, reduced Onion Architecture (Domain, Application, Infrastructure, Api, Web layers), local execution from source code only

**Out of Scope:**
- Microsoft Entra ID / corporate SSO — not needed to validate the core workflow; adds significant complexity out of proportion with MVP goals
- Azure deployment and cloud hosting — MVP is local only; cloud deployment is a post-validation decision
- Docker and CI/CD pipelines — local execution from source code is sufficient for acceptance
- Email and Microsoft Teams notifications — users consult the application directly for status
- Password reset and email verification — manual database reset or seeded accounts are used during review
- Account and role administration screens — seeded data and controlled registration are sufficient
- Vacation balance calculations — only calendar dates and decisions are recorded
- Holiday and working-day calendar calculations
- Overlapping request validation — adds policy complexity deferred to a later phase
- File attachments on requests
- Reports, exports, and dashboards
- HR administration views
- Multi-level approvals and approval delegation
- Integrations with payroll, HR, calendar, or directory systems
- Data migration from current email and spreadsheet records
- Advanced audit logs beyond the core Approval record
- Automated backups — the SQLite file location is documented; manual copy is sufficient for the MVP

**Deferred (Won't v1):**
- Microsoft Entra ID / corporate SSO — deferred pending decision to promote VacaFlow to a production system
- Azure hosting, Docker, CI/CD — deferred until production promotion is decided
- Email and Teams notifications — deferred; in-app visibility is the accepted pattern for this phase
- Password reset and email verification flows — deferred; not required for controlled MVP review
- Account and role administration screens — deferred; seeded data is sufficient
- Vacation balance tracking — deferred; balance logic requires policy decisions not in scope
- Holiday and working-day calendar calculations — deferred
- Overlapping request detection — deferred; adds policy complexity
- File attachments, reporting, dashboards — deferred
- HR staff views, multi-level approvals, approval delegation — deferred
- Payroll, HR, calendar, and directory integrations — deferred
- Data migration from email and spreadsheet records — deferred

---

## 5. Constraints & Assumptions

### Constraints (Non-negotiable)

**Business Constraints:**
- The MVP must not evolve into a full HR platform — scope is limited to the core request lifecycle as defined above
- Acceptance requires a live end-to-end demonstration with real registered accounts covering the complete workflow: register → log in → create Draft → validate date rules → edit → submit → confirm non-editability → create a second Draft → cancel it → confirm Cancelled state → manager log in → approve or reject with comment → confirm manager is recorded → employee views final result
- Any bypass of authenticated-user identity, or approval by a non-manager, is a rejection condition
- Blocking defects found during the review window must be resolved before final acceptance; cosmetic issues may be deferred
- James Parker provides functional sign-off and may involve one additional manager and one additional employee to run the workflow before final acceptance
- The database must not be committed with real passwords and must not be publicly exposed

**Technical Constraints:**
- Application must run locally from source code — no Azure, cloud hosting, Docker, or CI/CD in this MVP
- SQLite is the required database; no server-based database
- Backend must be ASP.NET Core Minimal API
- Frontend must be Next.js with React
- Data access must use Entity Framework Core
- Architecture must follow a reduced Onion structure: Domain, Application, Infrastructure, Api, and Web layers
- Passwords must be stored as hashes — plain text storage is a rejection condition
- The API must derive the current user and responsible approver from the authenticated session; the frontend must never send a trusted employee or approver identifier for business decisions
- The database must be generated or migrated automatically on startup so a reviewer can start the API and receive initial seeded data without manual steps
- Seeded data must include the three absence types (Vacation, Personal Leave, Sick Leave) and at least one manager account

**Legal Constraints:**
- For this MVP, no formal privacy notice or consent flow is required inside the application
- The application stores basic employee identity data (name, email) and absence request data (dates, reasons, approval comments); the database file must not be publicly exposed and must not be committed with real passwords
- If VacaFlow is promoted to a production system, privacy policy, data retention rules, and formal compliance requirements must be revisited as a separate scope decision before promotion

### Assumptions (If these change, project changes)

| Assumption | Impact if Wrong | Validation Method | Owner | Status |
|------------|-----------------|-------------------|-------|--------|
| James Parker and one to two additional reviewers are available to conduct the live acceptance demonstration within the agreed review window | Acceptance is delayed; delivery timeline extends | Confirm availability and reserve review session before development begins | Emily Harrison | Not Validated |
| SQLite is sufficient for the data volume and concurrency expected during local review (small number of concurrent users — reviewer team only) | Database choice must be reconsidered; architectural rework required | Confirm that the review involves no more than five concurrent users; no production load is expected | Emily Harrison | Not Validated |
| The development team has local environments capable of running Next.js and ASP.NET Core simultaneously without additional infrastructure setup | Developer onboarding time increases; local execution prerequisite fails | Verify local environment prerequisites with the development team before Sprint 1 | Emily Harrison | Not Validated |
| The current informal process does not have undocumented approval rules or policy exceptions that the MVP's fixed state machine would violate | The defined state transitions and business rules would not cover actual practice; rework required | James Parker to review and confirm all state transitions and business rules defined in the scope before development begins | James Parker | Not Validated |
| No GDPR, HIPAA, or equivalent regulatory framework applies to the storage of employee name, email, and absence reason data in this MVP context | A compliance assessment would be required before any further use of the system; legal review triggered | James Parker to confirm regulatory applicability, consulting IGS Solutions legal counsel as needed, before the MVP is used by real employees | James Parker | Not Validated |

---

## 6. Critical Information Gaps

| Gap | Impact if Not Resolved | Owner | Deadline |
|-----|------------------------|-------|----------|
| Manager-to-employee assignment model not fully defined: it is unclear whether one manager can be assigned to multiple employees and whether an employee can have only one assigned manager, or whether the assignment is ad hoc per request | Business rules around approval routing cannot be fully implemented; routing logic could be incorrect at acceptance | James Parker | Pre-kickoff |
| Acceptance session scheduling: no confirmed date, participants, or review environment for the live end-to-end demonstration | Delivery target cannot be set; development timeline is open-ended | Emily Harrison | Before Phase 2 approval |
| Number of employees expected to use the MVP during the review period: unclear whether this is limited to the reviewer team or extended to a broader employee group | SQLite concurrency assumption cannot be validated; scope creep risk if additional users are included without re-scoping | James Parker | Before Phase 2 approval |

---

## 7. Decision Framework

### Evaluation

| Criterion | Assessment | Justification |
|-----------|------------|---------------|
| **Strategic fit** | High | The project directly addresses a confirmed operational failure — a documented informal-approval incident — and an ongoing productivity drain. The scope is deliberately constrained to the minimum needed to eliminate these specific problems without expanding into a full HR platform |
| **Technical feasibility** | High, with one risk | The technology stack (Next.js, ASP.NET Core Minimal API, SQLite, EF Core, Onion Architecture) is well-established and appropriate for local execution. The primary risk is local environment setup for reviewers, which is mitigable through documented prerequisites |
| **Cost-benefit** | Favorable | The MVP eliminates a confirmed incident risk and reduces ongoing manager productivity drain with a tightly scoped deliverable. No cloud infrastructure cost is incurred. The deferred scope is explicitly bounded, limiting expansion risk |
| **Competitive position** | No exact alternative | The current alternative — email, Teams chat, and spreadsheets — provides none of the traceability, state enforcement, or accountability features the MVP delivers. No existing internal tool at IGS Solutions covers this workflow |
| **Team alignment** | Confirmed | James Parker has confirmed scope and acceptance criteria. Emily Harrison is engaged as Functional Analyst. Business rules and state transitions are documented and attributed to the Sponsor |

### Decision

**Recommendation:** Proceed with Conditions

**Conditions to Proceed:**
- Confirm manager-to-employee assignment model (one manager per employee, or ad hoc per request) — Owner: James Parker — Deadline: Pre-kickoff
- Confirm legal / regulatory applicability of storing employee personal and absence data in this MVP context, consulting legal counsel as needed — Owner: James Parker — Deadline: Pre-kickoff
- Reserve and confirm the acceptance session date and participants for the live end-to-end demonstration — Owner: Emily Harrison — Deadline: Before Phase 2 approval

**Rationale:**
VacaFlow addresses a real, documented operational failure with a well-scoped, technically feasible MVP and clear acceptance criteria. The three conditions above are information gaps that do not block development from starting but must be resolved before functional specification is finalized and before the acceptance session is scheduled.

---

## 8. Next Steps

- [ ] James Parker to confirm manager-to-employee assignment model — Owner: James Parker — Target: 2026-07-25
- [ ] James Parker to confirm legal / regulatory applicability of storing employee personal and absence data in this MVP context, consulting legal counsel as needed — Owner: James Parker — Target: 2026-07-25
- [ ] Emily Harrison to schedule and confirm acceptance session date and participants — Owner: Emily Harrison — Target: 2026-07-25
- [ ] Emily Harrison to verify local environment prerequisites with development team — Owner: Emily Harrison — Target: 2026-07-25
- [ ] James Parker to review and confirm all defined state transitions and business rules before Sprint 1 — Owner: James Parker — Target: 2026-07-28

**If Approved → Proceed to Phase 2:** Define — Functional Specification (Target: 2026-07-28)

---

## 9. Document Control

### Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-07-18 | Laura Hernandez (AI Assisted) | Initial draft |
| 1.1 | 2026-07-18 | Laura Hernandez (AI Assisted) | Added Cancelled flow to acceptance demonstration; aligned legal assumption validation method and ownership chain across Assumptions, Conditions, and Next Steps |

---
## Document Control

| Field | Value |
|-------|-------|
| Author | Laura Hernandez (AI Assisted) |
| Approval Authority | Product Owner (PM_OVERRIDE — bypassed Product Owner) |
| Status | Approved |
| Signature | ✅ SIGNED by Laura Hernandez (laura.hernandez@arroyoconsulting.net) on 2026-07-18 04:40:16 UTC |

*— End of document —*
