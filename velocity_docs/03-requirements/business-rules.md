# Business Rules Catalog: VacaFlow_14

**Author:** David Valdez, Laura Hernandez (AI Assisted)
**Date:** 2026-07-20
**Version:** 1.2
**Status:** Draft
**Document ID:** BR-001
**Project:** VacaFlow_14
**Organization:** IGS Solutions
**References:** SI-001 (Strategic Intake)

---

## 1. Overview

### 1.1 Purpose

This document catalogs the business rules governing the VacaFlow_14 system — an internal absence and vacation request management platform for IGS Solutions. It provides precise, implementable definitions of all business logic, constraints, decision criteria, and security invariants that the system must enforce, ensuring that developers, testers, and reviewers share a single authoritative reference.

### 1.2 Scope

This catalog covers:

- **User management rules** — registration, authentication, and identity enforcement
- **Role rules** — Employee and Manager role boundaries
- **Request lifecycle rules** — state transitions, ownership, and validity constraints
- **Manager review rules** — approval and rejection authority, assignment enforcement
- **Approval record rules** — creation, integrity, and identity sourcing
- **Final state rules** — immutability of terminal states
- **Security rules** — credential storage, session-derived identity, frontend trust boundary
- **Data integrity rules** — seeded reference data and manager assignment model
- **Request field rules** — field-level validation constraints (reason, absence type)

Rules are derived directly from the business context established in SI-001 and from the key processes and known rules provided by the project stakeholders.

### 1.3 Out of Scope

The following topics are explicitly excluded from this catalog (see SI-001 §4 — Out of Scope and Deferred):

- Vacation balance calculations
- Holiday and working-day calendar calculations
- Overlapping request detection
- Multi-level approvals and approval delegation
- Email and Teams notification rules
- Password reset and email verification flows
- Account and role administration rules
- Data retention and privacy compliance rules (deferred pending production promotion)

### 1.4 Rule Categories

| Category | Description | Example in VacaFlow |
|----------|-------------|---------------------|
| **Constraint** | Limits or restrictions that must be satisfied | End date cannot precede start date |
| **Computation** | Derived or calculated values | (No arithmetic computations in MVP scope) |
| **State Transition** | Valid state changes and their conditions | Only Draft requests can be submitted |
| **Authorization** | Who is permitted to perform an action | Only the request owner can cancel |
| **Inference** | Logical conclusions derived from data | Manager assignment is read from Employee record |
| **Security** | Data protection and identity enforcement | Passwords stored as hashes; identity from session |
| **Timing** | Date and time validity conditions | Start date cannot be in the past |

---

## 2. Rule Template

Every business rule in this catalog follows the structure below:

| Field | Description |
|-------|-------------|
| **Rule ID** | Unique identifier in format `BR-{CATEGORY}-{NNN}` |
| **Name** | Short, descriptive rule name |
| **Category** | One of: Constraint, State Transition, Authorization, Inference, Security, Timing |
| **Priority** | High / Medium / Low |
| **Source** | Origin of the rule (Stakeholder, Security Policy, System Design) |
| **Description** | Plain-language statement of what the rule enforces |
| **Condition** | When the rule is evaluated |
| **Rule Logic** | Formal or pseudo-code expression of the rule |
| **Action** | Outcome when condition is met or violated |
| **Exceptions** | Any cases where the rule does not apply |
| **Related Requirements** | References to predecessor documents |
| **Examples** | Illustrative scenarios |

---

## 3. User Management Rules

### BR-USER-001: Employee Registration

| Field | Value |
|-------|-------|
| **Rule ID** | BR-USER-001 |
| **Name** | Employee Self-Registration |
| **Category** | Constraint |
| **Priority** | High |
| **Source** | SI-001 §4 (In Scope) |

**Description:**
Any person may register a new account by providing a name, email address, password, and a role selection of either Employee or Manager. Registration is open — no admin approval is required for the MVP.

**Condition:**
When a registration request is submitted.

**Rule Logic:**
```
REQUIRED fields: name, email, password, role
role MUST be one of: {Employee, Manager}
email MUST be unique across all registered accounts
password MUST satisfy BR-SEC-001 (hashing requirement)
```

**Action:**
- All fields valid and email unique → create account, return success
- Missing required field → reject with field-level validation error
- Duplicate email → reject with "Email already registered" error

**Exceptions:**
None. All registrations follow the same path.

**Related Requirements:** SI-001 §4

---

### BR-USER-002: Authenticated Session Identity

| Field | Value |
|-------|-------|
| **Rule ID** | BR-USER-002 |
| **Name** | Session-Derived Identity |
| **Category** | Security |
| **Priority** | High |
| **Source** | SI-001 §5 (Technical Constraints) |

**Description:**
The API must derive the current user's identity exclusively from the authenticated session. The frontend must never send an employee identifier or approver identifier as a trusted input for business decisions. Any identity claim originating from the request body or query string is ignored for authorization and business logic purposes.

**Condition:**
On every API request that requires user identity for a business decision (request ownership checks, approval authority checks, record creation).

**Rule Logic:**
```
current_user = SESSION.authenticated_user
IF request_body contains employee_id OR approver_id
  IGNORE those values for all business decisions
  Use current_user exclusively
```

**Action:**
- Session has authenticated user → use session identity for all decisions
- Session is unauthenticated → return 401 Unauthorized

**Exceptions:**
None. This rule is absolute.

**Related Requirements:** SI-001 §4, §5 (Technical Constraints)

---

### BR-USER-003: Role Boundary Enforcement

| Field | Value |
|-------|-------|
| **Rule ID** | BR-USER-003 |
| **Name** | Role-Based Access Boundaries |
| **Category** | Authorization |
| **Priority** | High |
| **Source** | SI-001 §4 (In Scope) |

**Description:**
Two roles exist in the system: Employee and Manager. Actions available to each role are distinct for role-specific operations. A user who registered as Employee cannot perform Manager-exclusive actions. However, a Manager, when acting as the owner of their own absence requests, has access to the same request management actions as an Employee (create, edit, submit, cancel own requests), subject to the self-approval prohibition defined in BR-MGR-004.

**Condition:**
Before executing any role-restricted operation.

**Rule Logic:**
```
Employee-exclusive actions:
  - View own request history and final decisions

Manager-exclusive actions:
  - View Submitted requests assigned to them
  - Approve or reject a Submitted request assigned to them

Shared actions (available to both Employee and Manager roles):
  - Register, log in, log out
  - Create, edit, submit, and cancel own requests
    (Manager acting as requestor is subject to the same rules as Employee;
     see BR-REQ-004 for ownership enforcement and BR-MGR-004 for
     self-approval prohibition)
```

**Action:**
- Correct role for the operation → allow
- Employee attempting Manager-exclusive action → return 403 Forbidden
- Manager acting on own requests as requestor → allow (same as Employee path)

**Exceptions:**
- A Manager acting as the owner/requestor of their own absence requests is permitted to create, edit, submit, and cancel those requests under the same rules that apply to an Employee. This does not grant them any approval authority over their own requests (see BR-MGR-004). No other role escalation or delegation is supported in the MVP.

**Related Requirements:** SI-001 §4, BR-MGR-004

---

## 4. Request Lifecycle Rules

### 4.1 State Machine

VacaFlow enforces a strict, server-side state machine for all absence requests. The valid states and transitions are:

```
[Draft] ──submit──→ [Submitted] ──approve──→ [Approved]  (final)
   │                    │
   │                    └──reject──→ [Rejected]  (final)
   │
   └──cancel──→ [Cancelled]  (final — from Draft)
                    ↑
               [Submitted] ──cancel──→ [Cancelled]  (final — from Submitted)
```

**Terminal states:** Approved, Rejected, Cancelled. No further transitions are permitted from any terminal state.

---

### BR-REQ-001: Request Date — End Date Not Before Start Date

| Field | Value |
|-------|-------|
| **Rule ID** | BR-REQ-001 |
| **Name** | End Date Must Not Precede Start Date |
| **Category** | Constraint |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules |

**Description:**
The end date of an absence request must be equal to or later than the start date.

**Condition:**
When a Draft request is created or edited, and when a Draft request is submitted.

**Rule Logic:**
```
IF request.end_date < request.start_date
THEN reject WITH "End date cannot be earlier than start date"
```

**Action:**
- `end_date >= start_date` → validation passes
- `end_date < start_date` → reject operation with validation error

**Exceptions:**
None.

**Related Requirements:** SI-001 §4

**Examples:**

| Start Date | End Date | Valid | Reason |
|------------|----------|-------|--------|
| 2026-08-01 | 2026-08-05 | ✅ | End after start |
| 2026-08-01 | 2026-08-01 | ✅ | Single-day request |
| 2026-08-05 | 2026-08-01 | ❌ | End precedes start |

---

### BR-REQ-002: Request Date — Start Date Not in the Past

| Field | Value |
|-------|-------|
| **Rule ID** | BR-REQ-002 |
| **Name** | Start Date Cannot Be in the Past |
| **Category** | Timing |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules |

**Description:**
The start date of an absence request must be today or a future date. Requests for past dates are not accepted.

**Condition:**
When a Draft request is created or edited, and when a Draft request is submitted.

**Rule Logic:**
```
today = SYSTEM.current_date  (server-side; not from client)

IF request.start_date < today
THEN reject WITH "Start date cannot be in the past"
```

**Action:**
- `start_date >= today` → validation passes
- `start_date < today` → reject operation with validation error

**Exceptions:**
None for the MVP. Date is always evaluated server-side using system time.

**Related Requirements:** SI-001 §4

**Examples:**

| Today | Start Date | Valid | Reason |
|-------|------------|-------|--------|
| 2026-07-20 | 2026-07-20 | ✅ | Same day |
| 2026-07-20 | 2026-07-25 | ✅ | Future date |
| 2026-07-20 | 2026-07-19 | ❌ | Yesterday |

---

### BR-REQ-003: Only Draft Requests Can Be Edited

| Field | Value |
|-------|-------|
| **Rule ID** | BR-REQ-003 |
| **Name** | Edit Restricted to Draft State |
| **Category** | State Transition |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules |

**Description:**
A request may only be modified when it is in the Draft state. Once a request has been submitted, approved, rejected, or cancelled, its data is immutable.

**Condition:**
When an edit operation is attempted on a request.

**Rule Logic:**
```
IF request.status != Draft
THEN reject edit WITH "Only Draft requests can be edited"
```

**Action:**
- `status == Draft` → allow edit
- `status != Draft` → reject with 409 Conflict or 422 Unprocessable Entity

**Exceptions:**
None. This rule applies regardless of who is attempting the edit.

**Related Requirements:** SI-001 §4

---

### BR-REQ-004: Only the Request Owner Can Edit, Submit, or Cancel

| Field | Value |
|-------|-------|
| **Rule ID** | BR-REQ-004 |
| **Name** | Request Ownership — Edit, Submit, Cancel |
| **Category** | Authorization |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules |

**Description:**
Only the user who created a request may edit, submit, or cancel it. Identity is derived from the authenticated session (see BR-USER-002); the session user must match the request's owner. This applies equally to Employee-role and Manager-role users acting as requestors.

**Condition:**
When an edit, submit, or cancel operation is attempted on a request.

**Rule Logic:**
```
current_user = SESSION.authenticated_user
request_owner = request.created_by

IF current_user.id != request_owner.id
THEN reject WITH 403 Forbidden
```

**Action:**
- `current_user == request_owner` → allow operation (subject to state rules)
- `current_user != request_owner` → reject with 403 Forbidden

**Exceptions:**
None. Manager role does not grant authority over requests owned by other employees.

**Related Requirements:** SI-001 §4, BR-USER-003

---

### BR-REQ-005: Cancellation Is Available from Draft and Submitted States Only

| Field | Value |
|-------|-------|
| **Rule ID** | BR-REQ-005 |
| **Name** | Cancellation Valid from Draft or Submitted Only |
| **Category** | State Transition |
| **Priority** | High |
| **Source** | SI-001 §4 (In Scope), Stakeholder — Known Rules |

**Description:**
An employee (or a Manager acting as requestor) may cancel a request that is in Draft or Submitted state. A request that has already reached a terminal state (Approved, Rejected, Cancelled) cannot be cancelled again.

**Condition:**
When a cancel operation is attempted on a request.

**Rule Logic:**
```
IF request.status IN {Draft, Submitted} AND current_user == request.created_by
  THEN set request.status = Cancelled
ELSE IF request.status IN {Approved, Rejected, Cancelled}
  THEN reject WITH "Request is in a final state and cannot be cancelled"
```

**Action:**
- `status IN {Draft, Submitted}` and owner match → transition to Cancelled
- `status IN {Approved, Rejected, Cancelled}` → reject with 409 Conflict

**Exceptions:**
None.

**Related Requirements:** SI-001 §4, BR-REQ-004, BR-STATE-001

---

### BR-REQ-006: Submission Transitions Draft to Submitted

| Field | Value |
|-------|-------|
| **Rule ID** | BR-REQ-006 |
| **Name** | Submit Transitions Draft to Submitted |
| **Category** | State Transition |
| **Priority** | High |
| **Source** | SI-001 §4 (In Scope) |

**Description:**
Submitting a request moves it from Draft to Submitted, making it visible to the assigned manager for review. All date validations (BR-REQ-001, BR-REQ-002) and field validations (BR-FIELD-001) are re-evaluated at submission time.

**Condition:**
When an employee (or Manager acting as requestor) submits a Draft request.

**Rule Logic:**
```
IF request.status != Draft
  THEN reject WITH "Only Draft requests can be submitted"

VALIDATE BR-REQ-001 (end_date >= start_date)
VALIDATE BR-REQ-002 (start_date >= today)
VALIDATE BR-FIELD-001 (reason present and within length limit)

IF all validations pass
  THEN set request.status = Submitted
```

**Action:**
- Status is Draft and all validations pass → transition to Submitted
- Status is not Draft → reject
- Any validation fails → reject with specific validation error

**Exceptions:**
None.

**Related Requirements:** SI-001 §4, BR-REQ-001, BR-REQ-002, BR-REQ-003, BR-REQ-004, BR-FIELD-001

---

### BR-FIELD-001: Request Reason Field Validation

| Field | Value |
|-------|-------|
| **Rule ID** | BR-FIELD-001 |
| **Name** | Request Reason — Required and Length-Constrained |
| **Category** | Constraint |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules; implied by SI-001 §4 and functional specification acceptance criteria |

**Description:**
The `reason` field of an absence request is mandatory. It must be provided and must not exceed 500 characters. This field provides the context for the manager's review decision and forms part of the permanent request record. An empty or missing reason is not accepted when creating or editing a Draft request, and the constraint is re-validated at submission time.

**Condition:**
When a Draft request is created or edited, and when a Draft request is submitted.

**Rule Logic:**
```
IF reason IS NULL OR reason.trim() == ""
  THEN reject WITH "A reason for the absence request is required"

IF reason.length > 500
  THEN reject WITH "Reason must not exceed 500 characters"
```

**Action:**
- Reason present and within 500 characters → validation passes
- Reason absent or empty → reject with validation error
- Reason exceeds 500 characters → reject with validation error

**Exceptions:**
None. The reason field is mandatory for all absence request types.

**Related Requirements:** SI-001 §4, BR-REQ-006

**Examples:**

| Reason Value | Valid | Reason |
|--------------|-------|--------|
| "Annual family vacation" | ✅ | Non-empty, within limit |
| "Doctor appointment and recovery" | ✅ | Non-empty, within limit |
| "" (empty string) | ❌ | Empty reason not accepted |
| *(null / not provided)* | ❌ | Field is required |
| *(501+ character string)* | ❌ | Exceeds maximum length |

---

## 5. Manager Review Rules

### BR-MGR-001: Only Submitted Requests Can Be Approved or Rejected

| Field | Value |
|-------|-------|
| **Rule ID** | BR-MGR-001 |
| **Name** | Approve/Reject Restricted to Submitted State |
| **Category** | State Transition |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules |

**Description:**
A manager may only approve or reject a request that is currently in the Submitted state. Requests in any other state — including Draft, Approved, Rejected, and Cancelled — are not actionable.

**Condition:**
When a manager attempts to approve or reject a request.

**Rule Logic:**
```
IF request.status != Submitted
THEN reject WITH "Only Submitted requests can be approved or rejected"
```

**Action:**
- `status == Submitted` → allow approval or rejection (subject to BR-MGR-002, BR-MGR-003)
- `status != Submitted` → reject with 409 Conflict

**Exceptions:**
None.

**Related Requirements:** SI-001 §4

---

### BR-MGR-002: Only a Manager Role Can Approve or Reject

| Field | Value |
|-------|-------|
| **Rule ID** | BR-MGR-002 |
| **Name** | Manager Role Required for Approval or Rejection |
| **Category** | Authorization |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules |

**Description:**
Only users registered with the Manager role may approve or reject absence requests. Employees are not permitted to approve or reject any request, including their own.

**Condition:**
When an approve or reject operation is attempted.

**Rule Logic:**
```
current_user = SESSION.authenticated_user

IF current_user.role != Manager
THEN reject WITH 403 Forbidden
```

**Action:**
- `current_user.role == Manager` → allow (subject to BR-MGR-003, BR-MGR-004)
- `current_user.role == Employee` → reject with 403 Forbidden

**Exceptions:**
None.

**Related Requirements:** SI-001 §4, BR-USER-003

---

### BR-MGR-003: Manager Can Only Act on Requests of Assigned Employees

| Field | Value |
|-------|-------|
| **Rule ID** | BR-MGR-003 |
| **Name** | Manager Assignment Scope Enforcement |
| **Category** | Authorization |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules |

**Description:**
A manager may only approve or reject requests submitted by employees who are assigned to them. Assignment is read from the Employee record stored in the database. The manager has no authority over requests from employees assigned to other managers.

**Condition:**
When a manager attempts to approve or reject a request.

**Rule Logic:**
```
current_manager = SESSION.authenticated_user
request_owner = request.created_by

assigned_manager = DATABASE.Employee
                     .WHERE(id == request_owner.id)
                     .SELECT(manager_id)

IF assigned_manager != current_manager.id
THEN reject WITH 403 Forbidden ("You are not the assigned manager for this employee")
```

**Action:**
- `assigned_manager == current_manager.id` → allow (subject to BR-MGR-001, BR-MGR-004)
- `assigned_manager != current_manager.id` → reject with 403 Forbidden

**Exceptions:**
None. Manager assignment is a hard enforcement, not advisory.

**Related Requirements:** SI-001 §4, §6 (Critical Information Gaps — manager assignment model)

---

### BR-MGR-004: A Manager Cannot Approve or Reject Their Own Request

| Field | Value |
|-------|-------|
| **Rule ID** | BR-MGR-004 |
| **Name** | Self-Approval Prohibition |
| **Category** | Authorization |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules |

**Description:**
A Manager who has submitted an absence request for themselves cannot act as the approving manager for that same request. This prohibition applies unconditionally regardless of whether the Manager is also listed as the assigned manager for their own employee record. Self-approval is a hard rejection condition for the MVP (see SI-001 §4).

**Condition:**
When a manager attempts to approve or reject a request.

**Rule Logic:**
```
current_manager = SESSION.authenticated_user
request_owner = request.created_by

IF current_manager.id == request_owner.id
THEN reject WITH 403 Forbidden ("A manager cannot approve or reject their own request")
```

**Action:**
- `current_manager.id != request_owner.id` → allow (subject to other BR-MGR rules)
- `current_manager.id == request_owner.id` → reject with 403 Forbidden

**Exceptions:**
None. The self-approval prohibition is absolute. A Manager acting as requestor on their own absence request is subject to all Employee-path rules (BR-USER-003) but must have their requests approved by a different, duly assigned Manager.

**Related Requirements:** SI-001 §4, BR-USER-003

---

## 6. Approval Record Rules

### BR-APPR-001: Every Decision Creates Exactly One Approval Record

| Field | Value |
|-------|-------|
| **Rule ID** | BR-APPR-001 |
| **Name** | Approval Record Mandatory on Decision |
| **Category** | Constraint |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules, SI-001 §2 (Value Proposition) |

**Description:**
When a manager approves or rejects a Submitted request, the system must create exactly one Approval record. The creation of this record is atomic with the state transition of the request — both must succeed or both must fail. A request transition without a corresponding Approval record, or an Approval record without a corresponding state transition, is an invalid system state.

**Condition:**
When an approval or rejection operation succeeds (all other BR-MGR rules satisfied).

**Rule Logic:**
```
BEGIN TRANSACTION
  SET request.status = {Approved | Rejected}
  CREATE Approval {
    request_id:       request.id,
    decision:         {Approved | Rejected},
    decided_by:       SESSION.authenticated_user.id,  -- always from session
    decided_at:       SYSTEM.current_timestamp,
    comment:          manager_provided_comment (optional)
  }
  ASSERT COUNT(Approval WHERE request_id = request.id) == 1
COMMIT
```

**Action:**
- Transaction succeeds → request status updated, one Approval record created
- Any failure → rollback entire transaction; no partial state

**Exceptions:**
None. The one-record constraint is absolute.

**Related Requirements:** SI-001 §2, §4, BR-APPR-002

---

### BR-APPR-002: Responsible Approver Derived from Session

| Field | Value |
|-------|-------|
| **Rule ID** | BR-APPR-002 |
| **Name** | Approver Identity Sourced from Authenticated Session |
| **Category** | Security |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules, SI-001 §5 (Technical Constraints) |

**Description:**
The `decided_by` field of the Approval record must always be populated with the identity of the authenticated manager from the server-side session. The frontend must never supply or influence the approver identity recorded in the Approval record.

**Condition:**
When the Approval record is created as part of a manager decision.

**Rule Logic:**
```
approval.decided_by = SESSION.authenticated_user.id
-- The following is ALWAYS ignored for this field:
-- request_body.approver_id, query_params.manager_id, any client-supplied identity
```

**Action:**
- Session identity is available → record it as `decided_by`
- Session is unauthenticated → operation is blocked at the authentication layer (401); this rule is never reached

**Exceptions:**
None. Client-supplied approver identity is never accepted.

**Related Requirements:** SI-001 §4, §5, BR-USER-002, BR-APPR-001

---

## 7. Final State Rules

### BR-STATE-001: Terminal States Are Immutable

| Field | Value |
|-------|-------|
| **Rule ID** | BR-STATE-001 |
| **Name** | No Further Transitions from Terminal States |
| **Category** | State Transition |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules |

**Description:**
Once a request reaches a terminal state — Approved, Rejected, or Cancelled — no further state transitions are permitted. The request record and its associated Approval record (where applicable) are effectively immutable from a business process perspective.

**Condition:**
When any state-changing operation (edit, submit, cancel, approve, reject) is attempted on a request.

**Rule Logic:**
```
TERMINAL_STATES = {Approved, Rejected, Cancelled}

IF request.status IN TERMINAL_STATES
THEN reject ALL state-changing operations
  WITH "This request is in a final state and cannot be modified"

PERMITTED on terminal-state requests:
  - Read / view the request and its Approval record
```

**Action:**
- `status IN TERMINAL_STATES` → reject all write operations with 409 Conflict
- `status NOT IN TERMINAL_STATES` → evaluate applicable transition rules

**Exceptions:**
None. Read operations are always permitted regardless of state.

**Related Requirements:** SI-001 §4, BR-REQ-003, BR-REQ-005

**State Transition Matrix:**

| Current State | Edit | Submit | Cancel | Approve | Reject |
|---------------|------|--------|--------|---------|--------|
| Draft | ✅ | ✅ | ✅ | ❌ | ❌ |
| Submitted | ❌ | ❌ | ✅ | ✅ | ✅ |
| Approved | ❌ | ❌ | ❌ | ❌ | ❌ |
| Rejected | ❌ | ❌ | ❌ | ❌ | ❌ |
| Cancelled | ❌ | ❌ | ❌ | ❌ | ❌ |

---

## 8. Security Rules

### BR-SEC-001: Passwords Must Be Stored as Hashes

| Field | Value |
|-------|-------|
| **Rule ID** | BR-SEC-001 |
| **Name** | Password Hashing Requirement |
| **Category** | Security |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules, SI-001 §5 (Technical Constraints) |

**Description:**
Passwords must never be stored in plain text. Every password — whether set at registration or updated thereafter — must be stored exclusively as a cryptographic hash using a suitable algorithm (e.g., bcrypt, Argon2, or PBKDF2). Storing, logging, or transmitting a plain-text password at any point in the system is a rejection condition for the MVP.

**Condition:**
Whenever a password is set, changed, or stored.

**Rule Logic:**
```
ON password_provided:
  hashed_password = HASH(password, salt)  -- using bcrypt / Argon2 / PBKDF2
  STORE hashed_password ONLY
  NEVER store OR log plain_text_password

ON login:
  VERIFY(provided_password, stored_hash)
  RETURN authentication result
```

**Action:**
- Compliant → hash stored, plain text discarded
- Non-compliant (plain text stored) → automatic MVP rejection condition per SI-001 §5

**Exceptions:**
None.

**Related Requirements:** SI-001 §4, §5

---

### BR-SEC-002: Database Must Not Contain Real Passwords and Must Not Be Publicly Exposed

| Field | Value |
|-------|-------|
| **Rule ID** | BR-SEC-002 |
| **Name** | Database Credential and Exposure Protection |
| **Category** | Security |
| **Priority** | High |
| **Source** | SI-001 §5 (Technical Constraints) |

**Description:**
The SQLite database file must not be committed to source control with real passwords (even hashed) originating from real individuals, and must not be publicly accessible. Seeded accounts must use controlled, non-real credentials. The database file location must be documented so reviewers can locate it for manual backup; it must not be placed in a publicly accessible directory.

**Condition:**
Whenever the database file is created, seeded, committed, or deployed.

**Rule Logic:**
```
seeded_accounts MUST use controlled, non-production credentials
database_file MUST NOT be committed with real user credentials
database_file MUST NOT be accessible via public URL or shared path
database_file location MUST be documented in project README
```

**Action:**
- Compliant → database file is private and contains only controlled seed data
- Non-compliant → security incident; constitutes a data exposure risk

**Exceptions:**
None for the MVP.

**Related Requirements:** SI-001 §5

---

## 9. Data Integrity Rules

### BR-DATA-001: Absence Types Are Seeded and Read-Only

| Field | Value |
|-------|-------|
| **Rule ID** | BR-DATA-001 |
| **Name** | Absence Type Catalog — Seeded, No Maintenance Screen |
| **Category** | Constraint |
| **Priority** | Medium |
| **Source** | Stakeholder — Known Rules |

**Description:**
The three absence types — Vacation, Personal Leave, and Sick Leave — are seeded into the database on startup. No administration screen or API endpoint for creating, editing, or deleting absence types is required or permitted in the MVP. Employees (and Managers acting as requestors) select from these three types when creating a request.

**Condition:**
On application startup; when any user creates or edits a request.

**Rule Logic:**
```
ON startup:
  SEED AbsenceType [
    { id: 1, name: "Vacation" },
    { id: 2, name: "Personal Leave" },
    { id: 3, name: "Sick Leave" }
  ]
  -- IF already seeded, skip (idempotent seed)

ON request creation/edit:
  request.absence_type_id MUST reference a valid AbsenceType.id
  -- No free-text absence type entry is accepted
```

**Action:**
- Valid `absence_type_id` → allow
- Invalid or missing `absence_type_id` → reject with validation error

**Exceptions:**
None. The absence type catalog is fixed for the MVP.

**Related Requirements:** SI-001 §4

---

### BR-DATA-002: Manager Assignment Stored on Employee Record

| Field | Value |
|-------|-------|
| **Rule ID** | BR-DATA-002 |
| **Name** | Manager Assignment via Employee Record |
| **Category** | Inference |
| **Priority** | High |
| **Source** | Stakeholder — Known Rules, SI-001 §6 (Critical Information Gaps) |

**Description:**
The assignment of a manager to an employee is stored as an attribute of the Employee record (e.g., a `manager_id` foreign key). When the API evaluates approval authority, it reads this field directly from the database — it does not rely on any client-supplied value or session-derived inference beyond confirming the current user's identity.

**Condition:**
When a manager attempts to view, approve, or reject a request; when the system determines which Submitted requests are visible to a given manager.

**Rule Logic:**
```
employee.manager_id = ID of the Manager assigned to this employee
  -- stored at registration time or by an authorized assignment process

ON manager login:
  SHOW Submitted requests WHERE:
    request.created_by.manager_id == SESSION.authenticated_user.id

ON approve/reject:
  VERIFY request.created_by.manager_id == SESSION.authenticated_user.id
    (see BR-MGR-003)
```

**Action:**
- `employee.manager_id` set and matches → allow
- `employee.manager_id` not set or mismatches → reject per BR-MGR-003

**Exceptions:**
The specific cardinality of the assignment (one manager per employee, one employee per manager, or one-to-many) is flagged as a critical information gap in SI-001 §6 and must be confirmed by James Parker before Sprint 1. The rule above assumes one manager per employee as the working model pending that confirmation.

**Related Requirements:** SI-001 §6, BR-MGR-003

---

## 10. Rule Summary

### By Category

| Category | Count | Rule IDs |
|----------|-------|----------|
| Constraint | 5 | BR-USER-001, BR-REQ-001, BR-APPR-001, BR-DATA-001, BR-FIELD-001 |
| State Transition | 5 | BR-REQ-003, BR-REQ-005, BR-REQ-006, BR-MGR-001, BR-STATE-001 |
| Authorization | 5 | BR-USER-003, BR-REQ-004, BR-MGR-002, BR-MGR-003, BR-MGR-004 |
| Security | 4 | BR-USER-002, BR-APPR-002, BR-SEC-001, BR-SEC-002 |
| Timing | 1 | BR-REQ-002 |
| Inference | 1 | BR-DATA-002 |
| **Total** | **21** | |

### By Priority

| Priority | Count | Rule IDs |
|----------|-------|----------|
| High | 20 | BR-USER-001, BR-USER-002, BR-USER-003, BR-REQ-001, BR-REQ-002, BR-REQ-003, BR-REQ-004, BR-REQ-005, BR-REQ-006, BR-FIELD-001, BR-MGR-001, BR-MGR-002, BR-MGR-003, BR-MGR-004, BR-APPR-001, BR-APPR-002, BR-STATE-001, BR-SEC-001, BR-SEC-002, BR-DATA-002 |
| Medium | 1 | BR-DATA-001 |
| Low | 0 | — |
| **Total** | **21** | |

### By Domain

| Domain | Count | Rules |
|--------|-------|-------|
| User Management | 3 | BR-USER-001, BR-USER-002, BR-USER-003 |
| Request Lifecycle | 8 | BR-REQ-001, BR-REQ-002, BR-REQ-003, BR-REQ-004, BR-REQ-005, BR-REQ-006, BR-FIELD-001, BR-STATE-001 |
| Manager Review | 4 | BR-MGR-001, BR-MGR-002, BR-MGR-003, BR-MGR-004 |
| Approval Record | 2 | BR-APPR-001, BR-APPR-002 |
| Security | 2 | BR-SEC-001, BR-SEC-002 |
| Data Integrity | 2 | BR-DATA-001, BR-DATA-002 |
| **Total** | **21** | |

---

## 11. Cross-Reference: Catalog IDs to Functional Specification (FRS) Business Rules

The following table maps each rule in this authoritative catalog to its corresponding entry in the Business Rules table of the Functional Requirements Specification (functional-spec.md §5). Use this table to navigate between the two documents without ambiguity.

| Catalog Rule ID | Catalog Rule Name | FRS Rule ID (functional-spec.md §5) |
|-----------------|-------------------|--------------------------------------|
| BR-REQ-001 | End Date Must Not Precede Start Date | BR-001 |
| BR-REQ-002 | Start Date Cannot Be in the Past | BR-002 |
| BR-REQ-003 | Edit Restricted to Draft State | BR-003 |
| BR-REQ-004 | Request Ownership — Edit, Submit, Cancel | BR-004 |
| BR-MGR-001 | Approve/Reject Restricted to Submitted State | BR-005 |
| BR-MGR-002 | Manager Role Required for Approval or Rejection | BR-006 |
| BR-MGR-003 | Manager Assignment Scope Enforcement | BR-007 (partial; also see BR-DATA-002) |
| BR-MGR-004 | Self-Approval Prohibition | BR-007 (self-approval clause) |
| BR-APPR-001 | Approval Record Mandatory on Decision | BR-008 |
| BR-STATE-001 | No Further Transitions from Terminal States | BR-009 |
| BR-USER-002 | Session-Derived Identity | BR-010 |
| BR-DATA-001 | Absence Type Catalog — Seeded, No Maintenance Screen | BR-011 |
| BR-SEC-001 | Password Hashing Requirement | BR-012 |
| BR-DATA-002 | Manager Assignment via Employee Record | BR-013 |
| BR-USER-001 | Employee Self-Registration | Implicitly covered in FR-AUTH-001 / registration rules |
| BR-USER-003 | Role-Based Access Boundaries | Implicitly covered across role-based FRs (FR-EMP-*, FR-MGR-*) |
| BR-REQ-005 | Cancellation Valid from Draft or Submitted Only | Covered under state machine in FRS §3 |
| BR-REQ-006 | Submit Transitions Draft to Submitted | Covered under state machine in FRS §3 |
| BR-FIELD-001 | Request Reason — Required and Length-Constrained | Implied by FR-EMP-001 acceptance criteria |
| BR-APPR-002 | Approver Identity Sourced from Authenticated Session | BR-010 (identity sourcing clause) |
| BR-SEC-002 | Database Credential and Exposure Protection | SI-001 §5 (Technical Constraints) |

> **Note:** Rules with FRS IDs mapped to "Implicitly covered" or "Covered under state machine" were not listed as standalone numbered rules in functional-spec.md §5 but are derivable from the functional requirements, use cases, or state machine diagrams in that document. This catalog is the authoritative source for all business rule definitions; functional-spec.md is the authoritative source for use cases and acceptance criteria.

---

## 12. Open Items and Risks

| Item | Impact | Owner | Deadline |
|------|--------|-------|----------|
| Manager-to-employee assignment cardinality not yet confirmed (SI-001 §6): BR-DATA-002 assumes one manager per employee pending James Parker's confirmation | Approval routing logic in BR-MGR-003 may require adjustment if many-to-one or ad-hoc assignment is confirmed | James Parker | Pre-Sprint 1 (target 2026-07-25 per SI-001 §8) |
| Reason field maximum length (500 characters) defined in BR-FIELD-001 based on standard practice: not yet explicitly confirmed by stakeholders | If a different limit is required by policy, BR-FIELD-001 and corresponding validation logic must be updated before Sprint 1 | James Parker / Emily Harrison | Pre-Sprint 1 |
| Privacy and data retention rules not in scope for MVP (SI-001 §5): BR-SEC-002 provides baseline file protection only | If VacaFlow is promoted to production, formal compliance rules must be defined before promotion | James Parker | Pre-production promotion |

---

## 13. Document Control

### Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-07-18 | David Valdez, Laura Hernandez (AI Assisted) | Initial catalog — 20 rules across 6 domains derived from SI-001 and stakeholder-provided known rules |
| 1.1 | 2026-07-18 | David Valdez, Laura Hernandez (AI Assisted) | Corrected Rule Summary counts: By Category totals updated to 20; BR-MGR-001 correctly placed under State Transition; BR-MGR-004 correctly placed under Authorization; By Priority High count corrected to 19; By Domain count column added with total of 20. Corrected Source citations for BR-USER-002 and BR-APPR-002 to reference SI-001 §5 (Technical Constraints). Corrected Open Items deadline for manager-assignment confirmation from 2026-07-28 to 2026-07-25, aligned with SI-001 §8 and functional-spec.md §10. |
| 1.2 | 2026-07-20 | David Valdez, Laura Hernandez (AI Assisted) | Addressed three reviewer feedback items: (1) Revised BR-USER-003 to clarify that a Manager acting as requestor on their own absence requests has the same access as an Employee for create/edit/submit/cancel operations, subject to BR-MGR-004 self-approval prohibition — exception clause added and description updated throughout. (2) Added new rule BR-FIELD-001 (Request Reason — Required and Length-Constrained) formalizing the mandatory reason field with a 500-character maximum, referenced from BR-REQ-006 and FIELD-001 validation in submission logic. (3) Added Section 11 (Cross-Reference: Catalog IDs to Functional Specification FRS Business Rules) mapping all 21 catalog rule IDs to their corresponding FRS IDs in functional-spec.md §5. Rule totals updated to 21 across all summary tables. |

---

## Approval

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Solution Architect | | | ⏳ Pending |
| Technical Lead | | | ⏳ Pending |
| Business Owner | | | ⏳ Pending |

---
## Document Control

| Field | Value |
|-------|-------|
| Author | David Valdez, Laura Hernandez (AI Assisted) |
| Approval Authority | Solution Architect (PM_OVERRIDE — bypassed Solution Architect) |
| Status | Approved |
| Signature | ✅ SIGNED by Laura Hernandez (laura.hernandez@arroyoconsulting.net) on 2026-07-20 16:16:55 UTC |

*— End of document —*
