# Business Rules Catalog: VacaFlow_14

**Author:** David Valdez, Laura Hernandez (AI Assisted)
**Date:** 2026-07-18
**Version:** 1.0
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
| **Source** | SI-001 §4 (Technical Constraints), Stakeholder — Known Rules |

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
Two roles exist in the system: Employee and Manager. Actions available to each role are mutually exclusive for role-specific operations. A user who registered as Employee cannot perform Manager actions and vice versa.

**Condition:**
Before executing any role-restricted operation.

**Rule Logic:**
```
Employee-exclusive actions:
  - Create, edit, submit, and cancel own requests
  - View own request history and final decisions

Manager-exclusive actions:
  - View Submitted requests assigned to them
  - Approve or reject a Submitted request assigned to them

Shared actions:
  - Register, log in, log out
```

**Action:**
- Correct role → allow operation
- Incorrect role → return 403 Forbidden

**Exceptions:**
None for the MVP. No role escalation or delegation is supported.

**Related Requirements:** SI-001 §4

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
| 2026-07-18 | 2026-07-18 | ✅ | Same day |
| 2026-07-18 | 2026-07-20 | ✅ | Future date |
| 2026-07-18 | 2026-07-17 | ❌ | Yesterday |

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
Only the employee who created a request may edit, submit, or cancel it. Identity is derived from the authenticated session (see BR-USER-002); the session user must match the request's owner.

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
None. Manager role does not override this restriction for employee-owned operations.

**Related Requirements:** SI-001 §4

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
An employee may cancel a request that is in Draft or Submitted state. A request that has already reached a terminal state (Approved, Rejected, Cancelled) cannot be cancelled again.

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
Submitting a request moves it from Draft to Submitted, making it visible to the assigned manager for review. All date validations (BR-REQ-001, BR-REQ-002) are re-evaluated at submission time.

**Condition:**
When an employee submits a Draft request.

**Rule Logic:**
```
IF request.status != Draft
  THEN reject WITH "Only Draft requests can be submitted"

VALIDATE BR-REQ-001 (end_date >= start_date)
VALIDATE BR-REQ-002 (start_date >= today)

IF all validations pass
  THEN set request.status = Submitted
```

**Action:**
- Status is Draft and all validations pass → transition to Submitted
- Status is not Draft → reject
- Any validation fails → reject with specific validation error

**Exceptions:**
None.

**Related Requirements:** SI-001 §4, BR-REQ-001, BR-REQ-002, BR-REQ-003, BR-REQ-004

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
A manager who has submitted an absence request for themselves — whether registered as Employee or as a dual-role scenario — cannot act as the approving manager for that same request. Self-approval is prohibited unconditionally.

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
None.

**Related Requirements:** SI-001 §4

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
| **Source** | Stakeholder — Known Rules, SI-001 §4 (Technical Constraints) |

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
| **Source** | SI-001 §5 (Legal Constraints) |

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
The three absence types — Vacation, Personal Leave, and Sick Leave — are seeded into the database on startup. No administration screen or API endpoint for creating, editing, or deleting absence types is required or permitted in the MVP. Employees select from these three types when creating a request.

**Condition:**
On application startup; when an employee creates or edits a request.

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
| Constraint | 4 | BR-USER-001, BR-REQ-001, BR-APPR-001, BR-DATA-001 |
| State Transition | 4 | BR-REQ-003, BR-REQ-005, BR-REQ-006, BR-STATE-001 |
| Authorization | 4 | BR-USER-003, BR-REQ-004, BR-MGR-002, BR-MGR-003 |
| Security | 4 | BR-USER-002, BR-APPR-002, BR-SEC-001, BR-SEC-002 |
| Timing | 1 | BR-REQ-002 |
| Inference | 1 | BR-DATA-002 |
| **Total** | **18** | |

### By Priority

| Priority | Count | Rule IDs |
|----------|-------|----------|
| High | 17 | BR-USER-001, BR-USER-002, BR-USER-003, BR-REQ-001, BR-REQ-002, BR-REQ-003, BR-REQ-004, BR-REQ-005, BR-REQ-006, BR-MGR-001, BR-MGR-002, BR-MGR-003, BR-MGR-004, BR-APPR-001, BR-APPR-002, BR-STATE-001, BR-SEC-001, BR-SEC-002, BR-DATA-002 |
| Medium | 1 | BR-DATA-001 |
| Low | 0 | — |

### By Domain

| Domain | Rules |
|--------|-------|
| User Management | BR-USER-001, BR-USER-002, BR-USER-003 |
| Request Lifecycle | BR-REQ-001, BR-REQ-002, BR-REQ-003, BR-REQ-004, BR-REQ-005, BR-REQ-006, BR-STATE-001 |
| Manager Review | BR-MGR-001, BR-MGR-002, BR-MGR-003, BR-MGR-004 |
| Approval Record | BR-APPR-001, BR-APPR-002 |
| Security | BR-SEC-001, BR-SEC-002 |
| Data Integrity | BR-DATA-001, BR-DATA-002 |

---

## 11. Open Items and Risks

| Item | Impact | Owner | Deadline |
|------|--------|-------|----------|
| Manager-to-employee assignment cardinality not yet confirmed (SI-001 §6): BR-DATA-002 assumes one manager per employee pending James Parker's confirmation | Approval routing logic in BR-MGR-003 may require adjustment if many-to-one or ad-hoc assignment is confirmed | James Parker | Pre-Sprint 1 (target 2026-07-28 per SI-001 §8) |
| Privacy and data retention rules not in scope for MVP (SI-001 §5): BR-SEC-002 provides baseline file protection only | If VacaFlow is promoted to production, formal compliance rules must be defined before promotion | James Parker | Pre-production promotion |

---

## 12. Document Control

### Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-07-18 | David Valdez, Laura Hernandez (AI Assisted) | Initial catalog — 18 rules across 6 domains derived from SI-001 and stakeholder-provided known rules |

---
## Document Control

| Field | Value |
|-------|-------|
| Author | David Valdez, Laura Hernandez (AI Assisted) |
| Approval Authority | Solution Architect |
| Status | Draft |
| Signature | ⏳ Pending — awaiting approval |

*— End of document —*
