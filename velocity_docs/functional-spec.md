# Functional Requirements Specification

**Project:** VacaFlow_14
**Document ID:** FRS-001
**Stage:** 02 — Define
**Author:** Laura Hernandez (AI Assisted)
**BSA/PO:** Laura Hernandez
**Related SI:** SI-001
**Date:** 2026-07-18
**Version:** 1.2
**Status:** Draft

---

## 1. System Overview

### 1.1 Purpose

VacaFlow is a web application that provides IGS Solutions employees and their managers with a structured, traceable system for submitting and deciding on vacation and absence requests. The system replaces an informal process conducted through email, Microsoft Teams chat, and spreadsheets — a process that has already produced a documented incident in which an employee acted on an informal chat reply as though it were a formal approval while no decision was ever recorded. VacaFlow enforces a defined request lifecycle with authenticated state transitions, links every approval or rejection decision to the authenticated manager who made it, and gives both employees and managers a single queryable source of truth for every request and its current state.

### 1.2 Scope

VacaFlow encompasses application-managed user registration and authentication, a full vacation and absence request lifecycle (Draft → Submitted → Approved / Rejected / Cancelled), employee self-service request management, and manager approval and rejection of submitted requests. The system runs locally from source code and is not deployed to any cloud environment in this version. Corporate SSO, email notifications, vacation balance calculations, holiday calendars, overlapping request validation, file attachments, reporting, and all administrative maintenance screens are explicitly excluded from this version.

### 1.3 Definitions

| Term | Definition |
|------|------------|
| Draft | Initial state of a request after creation; editable and cancellable by the owner |
| Submitted | State reached when the employee submits a Draft; request is read-only for the employee; awaits manager decision |
| Approved | Final state reached when the assigned manager approves a Submitted request; no further transitions permitted |
| Rejected | Final state reached when the assigned manager rejects a Submitted request; no further transitions permitted |
| Cancelled | Final state reached when the request owner cancels a Draft or Submitted request; no further transitions permitted |
| Absence Type | A seeded category for the nature of an absence (Vacation, Personal Leave, Sick Leave); not user-maintainable |
| Approval Record | A persisted database record created exactly once per approval or rejection decision, storing the authenticated manager identity, the decision, the optional comment, and the decision date |
| Authenticated Session | The server-side session that establishes and carries the current user's identity; the sole authoritative source for user identity in all business operations |
| Assigned Manager | The manager associated with the employee who owns the request; the only role authorized to approve or reject that request. Stored as a single manager reference on the Employee record, established via seed data or controlled setup — not a full organizational hierarchy, and not self-service |
| Employee | An application user with the Employee role who can manage their own absence requests |
| Manager | An application user with the Manager role who can review and decide on Submitted requests from employees assigned to them |
| Onion Architecture | The reduced layered architecture used in this project: Domain, Application, Infrastructure, Api, and Web layers |

---

## 2. User Roles & Personas

| Role ID | Role Name | Description | Access Level |
|---------|-----------|-------------|--------------|
| UR-01 | Employee | An IGS Solutions staff member who submits and manages their own vacation and absence requests. Registers an account, creates requests, tracks status, and views final decisions. | Create, Read, Edit, Submit, Cancel (own requests only) |
| UR-02 | Manager | An IGS Solutions manager who reviews and decides on Submitted requests from employees assigned to them. Can also submit and manage their own absence requests as a requester (subject to the same employee-role rules), but cannot approve or reject their own requests. | Read (assigned Submitted requests), Approve, Reject (assigned requests only); also holds Employee-level access for own requests |
| UR-03 | Developer / Test Administrator | Technical role responsible for seeding or setting up test data, including the Employee-to-Manager assignment. Not an end-user of the application; has no UI access for these operations. | Database-level / controlled setup only |

---

## 3. Functional Requirements

> Requirements are grouped by feature/module.
> Format: FR-[Feature Code]-[###]
> Priority: Must Have | Should Have | Won't v1

---

### 3.1 User Registration and Authentication

**Feature Description:** Application-managed registration and login using email and password. Passwords are stored as hashes. Supports two roles (Employee and Manager) selected at registration. The API derives the current user identity from the authenticated session — the frontend never sends a trusted user or approver identifier. At least one Manager account is seeded into the database on startup. Each Employee record stores a single assigned Manager reference, established via seed data or controlled setup — not through the registration flow or any administrative UI.

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| FR-AUTH-001 | The system shall allow a new user to register by providing their full name, email address, password, and role (Employee or Manager). | Must Have | Role is selected at registration; no admin step required. The Employee-to-Manager assignment is established separately via seed data or controlled setup — see FR-AUTH-010. |
| FR-AUTH-002 | The system shall store all passwords as cryptographic hashes; plain-text password storage is prohibited. | Must Have | Rejection condition per SI-001 §5 Technical Constraints |
| FR-AUTH-003 | The system shall allow a registered user to log in using their email address and password. | Must Have | |
| FR-AUTH-004 | The system shall allow an authenticated user to log out, terminating their session. | Must Have | |
| FR-AUTH-005 | The system shall expose a "current user" endpoint that returns the authenticated user's identity and role from the session. | Must Have | Frontend uses this; never trusts client-supplied identity |
| FR-AUTH-006 | The system shall prevent any business operation from proceeding unless the requesting user has a valid authenticated session. | Must Have | All business endpoints require authentication; see SI-001 §5 Technical Constraints |
| FR-AUTH-007 | The system shall seed at least one Manager account into the database on startup, available without manual insertion. | Must Have | Required for acceptance demonstration |
| FR-AUTH-008 | The system shall seed the three absence types (Vacation, Personal Leave, Sick Leave) into the database on startup. | Must Have | Seeded catalog; no maintenance screen provided |
| FR-AUTH-009 | The system shall apply database migrations automatically on startup so the application is ready for use without manual database setup steps. | Must Have | Reviewer must be able to start the API and receive seeded data immediately |
| FR-AUTH-010 | The system shall store a single assigned Manager reference on each Employee record. This reference is established via seed data or controlled setup — not through self-registration, and not through any administrative UI. | Must Have | Simple one-to-one reference, not an organizational hierarchy. Source: meeting transcript "Delivery Architecture and Acceptance" (resolves SI-001 §6 Gap: Manager-to-employee assignment model) |

#### Acceptance Criteria

**FR-AUTH-001:**
- Given a user is not registered, when they submit valid name, email, password, and role, then the system creates an account and the user can subsequently log in.
- Given a user submits a registration form with an email already in use, when the system processes the request, then registration is rejected with an appropriate error.

**FR-AUTH-002:**
- Given any registration or password storage event, when the system persists the password, then only the hash is stored and no plain-text representation is written to the database.

**FR-AUTH-003:**
- Given a registered user provides their correct email and password, when they attempt to log in, then the system establishes an authenticated session and returns the user's identity and role.
- Given a user provides an incorrect email or password, when they attempt to log in, then the system rejects the attempt without revealing which field was incorrect.

**FR-AUTH-004:**
- Given an authenticated user triggers logout, when the system processes the request, then the session is terminated and subsequent authenticated-only requests from that session are rejected.

**FR-AUTH-005:**
- Given an authenticated session exists, when the frontend calls the current-user endpoint, then the system returns the authenticated user's name, email, and role.
- Given no valid session exists, when the current-user endpoint is called, then the system returns an unauthenticated response.

**FR-AUTH-006:**
- Given any business operation endpoint is called without a valid authenticated session, when the system processes the request, then it returns an unauthorized response and performs no business action.

**FR-AUTH-007:**
- Given the API starts with an empty or freshly migrated database, when the startup completes, then at least one Manager account exists and can be used to log in without manual insertion.

**FR-AUTH-008:**
- Given the API starts with an empty or freshly migrated database, when the startup completes, then the absence types Vacation, Personal Leave, and Sick Leave exist in the catalog.

**FR-AUTH-009:**
- Given the source code is checked out and the API is started for the first time, when startup completes, then the database schema exists and seeded data is present without any manual migration or seed command.

**FR-AUTH-010:**
- Given an Employee record is created via seed data or controlled setup, when the record is persisted, then it includes a reference to exactly one assigned Manager.
- Given a Manager attempts to approve or reject a Submitted request, when the system checks authorization, then it reads the Employee's stored Manager reference — never a value supplied by the frontend — to confirm the manager is assigned to that employee.

---

### 3.2 Vacation and Absence Request Management (Employee)

**Feature Description:** Allows employees to create, edit, submit, and cancel their own vacation and absence requests. Each request records the owner, absence type, date range, reason, and current lifecycle state. Business rules governing date validity, editability, and state transitions are enforced by the API.

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| FR-EMP-001 | The system shall allow an authenticated Employee to create a new absence request specifying absence type, start date, end date, and reason; the request shall be saved in Draft state. | Must Have | |
| FR-EMP-002 | The system shall allow an authenticated Employee to edit their own Draft request, changing the absence type, start date, end date, or reason. | Must Have | Only Draft requests are editable |
| FR-EMP-003 | The system shall allow an authenticated Employee to submit their own Draft request, transitioning it to Submitted state. | Must Have | |
| FR-EMP-004 | The system shall allow an authenticated Employee to cancel their own Draft or Submitted request, transitioning it to Cancelled state. | Must Have | |
| FR-EMP-005 | The system shall make a Submitted, Approved, Rejected, or Cancelled request read-only for the employee — no edits or state changes other than cancellation of Submitted are permitted by the employee once submitted. | Must Have | Approved, Rejected, Cancelled are final |
| FR-EMP-006 | The system shall allow an authenticated Employee to view a list of all their own requests, including current state and, for decided requests, the final decision and optional manager comment. | Must Have | |
| FR-EMP-007 | The system shall reject any request whose end date is earlier than its start date, returning a validation error. | Must Have | Business rule BR-001 |
| FR-EMP-008 | The system shall reject any request whose start date is in the past, returning a validation error. | Must Have | Business rule BR-002 |

#### Acceptance Criteria

**FR-EMP-001:**
- Given an authenticated Employee selects an absence type, valid start and end dates, and provides a reason, when they create the request, then a new request exists in Draft state owned by that employee.
- Given an authenticated Employee omits a required field, when they attempt to create the request, then the system returns a validation error and no request is created.

**FR-EMP-002:**
- Given an Employee owns a request in Draft state, when they update the absence type, dates, or reason, then the changes are persisted and the request remains in Draft state.
- Given an Employee owns a request not in Draft state, when they attempt to edit it, then the system returns an error and no change is made.

**FR-EMP-003:**
- Given an Employee owns a request in Draft state, when they submit it, then the request transitions to Submitted state and becomes read-only for the employee.
- Given an Employee attempts to submit a request not in Draft state, then the system returns an error and no transition occurs.

**FR-EMP-004:**
- Given an Employee owns a request in Draft or Submitted state, when they cancel it, then the request transitions to Cancelled state.
- Given an Employee owns a request in Approved, Rejected, or Cancelled state, when they attempt to cancel it, then the system returns an error and no transition occurs.

**FR-EMP-005:**
- Given a request is in Submitted state, when the owning Employee attempts to edit any field, then the system returns an error and the request is unchanged.
- Given a request is in Approved, Rejected, or Cancelled state, when any state-change or edit operation is attempted, then the system rejects the operation.

**FR-EMP-006:**
- Given an authenticated Employee views their request list, when the list is returned, then it includes all their requests with current state; for Approved or Rejected requests, the decision and optional comment are visible.

**FR-EMP-007:**
- Given a request submission where the end date is earlier than the start date, when the system validates the request, then it returns a validation error and the request is not created or updated.

**FR-EMP-008:**
- Given a request submission where the start date is a date in the past relative to today, when the system validates the request, then it returns a validation error and the request is not created or updated.

---

### 3.3 Vacation and Absence Request Management (Manager as Requester)

**Feature Description:** A Manager also holds the requester role for their own absence requests, subject to the same rules as an Employee. This section covers Manager-as-requester behavior, which is identical to Employee behavior with the additional constraint that a Manager cannot approve or reject their own requests.

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| FR-MGR-REQ-001 | The system shall allow an authenticated Manager to create, edit, submit, and cancel their own absence requests under the same rules that apply to Employees. | Must Have | Manager holds dual role; employee-side rules apply to own requests |
| FR-MGR-REQ-002 | The system shall prevent a Manager from approving or rejecting a Submitted request that they own. | Must Have | Business rule BR-007; enforced by the API |

#### Acceptance Criteria

**FR-MGR-REQ-001:**
- Given an authenticated Manager creates a request, when they follow the same steps as an Employee, then a Draft request is created under their ownership and all employee-side rules apply identically.
- Given an authenticated Manager owns a Draft request, when they edit it, then the changes are persisted and the request remains in Draft state, matching the behavior defined for FR-EMP-002.
- Given an authenticated Manager owns a Draft or Submitted request, when they cancel it, then the request transitions to Cancelled state, matching the behavior defined for FR-EMP-004.

**FR-MGR-REQ-002:**
- Given a Manager has a Submitted request of their own, when they attempt to approve or reject it through any endpoint, then the system returns an error and no Approval record is created.

---

### 3.4 Manager Approval and Rejection

**Feature Description:** Allows managers to review Submitted requests from employees assigned to them and to approve or reject those requests with an optional comment. Every decision creates exactly one Approval record storing the authenticated manager as the responsible approver, the decision, the optional comment, and the decision date. The manager's identity is always derived from the authenticated session — the frontend never supplies an approver identifier. The assignment between a Manager and their employees is stored as a single Manager reference on each Employee record, established via seed data or controlled setup (FR-AUTH-010, BR-013).

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| FR-APPR-001 | The system shall allow an authenticated Manager to view a list of Submitted requests owned by employees assigned to them. | Must Have | Manager sees only their assigned employees' Submitted requests; assignment source: FR-AUTH-010 / BR-013 |
| FR-APPR-002 | The system shall allow an authenticated Manager to approve a Submitted request assigned to them, transitioning it to Approved state. | Must Have | |
| FR-APPR-003 | The system shall allow an authenticated Manager to reject a Submitted request assigned to them, transitioning it to Rejected state. | Must Have | |
| FR-APPR-004 | The system shall allow a Manager to include an optional free-text comment when approving or rejecting a request. | Must Have | Comment stored in the Approval record |
| FR-APPR-005 | The system shall create exactly one Approval record for each approval or rejection decision, containing: the request reference, the authenticated manager as responsible approver, the decision (Approved or Rejected), the optional comment, and the decision date. | Must Have | Business rule BR-008; Approval record is non-negotiable |
| FR-APPR-006 | The system shall derive the responsible approver identity exclusively from the authenticated session; the frontend must not supply a manager or approver identifier for this operation. | Must Have | SI-001 §5 Technical Constraints |
| FR-APPR-007 | The system shall prevent a Manager from approving or rejecting a request that is not in Submitted state. | Must Have | Business rule BR-005 |
| FR-APPR-008 | The system shall prevent a Manager from approving or rejecting a request they do not own the assignment for. | Must Have | Business rule BR-006; assignment source: FR-AUTH-010 / BR-013 |
| FR-APPR-009 | The system shall prevent a Manager from approving or rejecting their own request. | Must Have | Business rule BR-007 |

#### Acceptance Criteria

**FR-APPR-001:**
- Given an authenticated Manager views the review list, when the list is returned, then it contains only Submitted requests belonging to employees assigned to that manager, and no requests from employees assigned to other managers.

**FR-APPR-002:**
- Given a Manager selects a Submitted request assigned to them and chooses to approve it, when the system processes the action, then the request transitions to Approved state and an Approval record is created.

**FR-APPR-003:**
- Given a Manager selects a Submitted request assigned to them and chooses to reject it, when the system processes the action, then the request transitions to Rejected state and an Approval record is created.

**FR-APPR-004:**
- Given a Manager approves or rejects a request and includes a comment, when the action is processed, then the comment is stored in the Approval record and is visible to the employee.
- Given a Manager approves or rejects a request without a comment, when the action is processed, then the Approval record is created with an empty or null comment and the decision is recorded.

**FR-APPR-005:**
- Given any approval or rejection action completes successfully, when the database is inspected, then exactly one Approval record exists for that decision containing the request reference, the authenticated manager's identity, the decision, the optional comment, and the decision date.
- Given an approval action is attempted for a request that already has an Approval record, when the system processes the request, then it returns an error and no duplicate record is created.

**FR-APPR-006:**
- Given a Manager triggers an approval or rejection, when the system identifies the responsible approver, then it reads the identity exclusively from the authenticated session and ignores any approver identifier supplied in the request body.

**FR-APPR-007:**
- Given a Manager attempts to approve or reject a request not in Submitted state, when the system processes the request, then it returns an error and no Approval record is created.

**FR-APPR-008:**
- Given a Manager attempts to approve or reject a request belonging to an employee not assigned to them, when the system processes the request, then it returns an error and no Approval record is created.

**FR-APPR-009:**
- Given a Manager has a Submitted request that they own, when they attempt to approve or reject it through the manager review endpoint, then the system returns an error and no Approval record is created.

---

## 4. Use Cases

> High-level use cases describing user-system interactions.
> Format: UC-[###]

---

### UC-001: Employee Registers an Account

| Field | Value |
|-------|-------|
| **ID** | UC-001 |
| **Actor** | Unregistered user |
| **Goal** | Create an account to access VacaFlow |
| **Trigger** | User navigates to the registration screen |
| **Preconditions** | User does not have an existing account with the same email |
| **Postconditions** | A new user account exists with a hashed password and the selected role |

**Main Flow:**
1. User opens the registration screen.
2. User enters full name, email address, password, and selects a role (Employee or Manager).
3. User submits the form.
4. System validates that the email is not already registered and that all required fields are present.
5. System hashes the password and persists the new account.
6. System establishes an authenticated session and redirects to the appropriate home screen.

**Alternative Flows:**
- User selects Manager role: same flow; account is created with Manager role; seeded manager account also exists.

**Exceptions:**
- Email already registered: System returns a registration error; no account is created.
- Required field missing: System returns a validation error identifying the missing field; no account is created.

---

### UC-002: User Logs In

| Field | Value |
|-------|-------|
| **ID** | UC-002 |
| **Actor** | Registered Employee or Manager |
| **Goal** | Authenticate and access the application |
| **Trigger** | User navigates to the login screen |
| **Preconditions** | User has a registered account |
| **Postconditions** | An authenticated session exists; user is directed to their role-appropriate home screen |

**Main Flow:**
1. User enters their email and password on the login screen.
2. User submits the form.
3. System validates credentials against the stored hash.
4. System creates an authenticated session recording the user's identity and role.
5. System redirects the user to their home screen (employee request list or manager review list, as appropriate).

**Alternative Flows:**
- None.

**Exceptions:**
- Incorrect email or password: System returns an authentication error without specifying which field was wrong; no session is created.

---

### UC-003: Employee Creates a Draft Request

| Field | Value |
|-------|-------|
| **ID** | UC-003 |
| **Actor** | Employee (UR-01) |
| **Goal** | Record a new absence request for review |
| **Trigger** | Employee selects "New Request" on their request list screen |
| **Preconditions** | Employee is authenticated |
| **Postconditions** | A new request exists in Draft state owned by the employee |

**Main Flow:**
1. Employee opens the request creation form.
2. Employee selects an absence type from the seeded catalog (Vacation, Personal Leave, Sick Leave).
3. Employee enters start date, end date, and reason.
4. Employee submits the form.
5. System validates that the start date is not in the past and that the end date is not earlier than the start date.
6. System persists the request in Draft state with the authenticated employee as owner.
7. System returns the updated request list showing the new Draft.

**Alternative Flows:**
- None.

**Exceptions:**
- Start date is in the past: System returns a validation error; no request is created.
- End date is before start date: System returns a validation error; no request is created.
- Required field missing: System returns a validation error; no request is created.

---

### UC-004: Employee Edits a Draft Request

| Field | Value |
|-------|-------|
| **ID** | UC-004 |
| **Actor** | Employee (UR-01) |
| **Goal** | Correct or update a Draft request before submission |
| **Trigger** | Employee selects "Edit" on a Draft request in their list |
| **Preconditions** | Employee is authenticated; the selected request is in Draft state and owned by the employee |
| **Postconditions** | The request reflects the updated values and remains in Draft state |

**Main Flow:**
1. Employee opens the edit form for the Draft request.
2. Employee modifies one or more fields (absence type, start date, end date, reason).
3. Employee submits the updated form.
4. System validates the updated date values against business rules.
5. System persists the changes; request remains in Draft state.

**Alternative Flows:**
- None.

**Exceptions:**
- Request is not in Draft state: System returns an error; no changes are made.
- Updated dates violate business rules: System returns a validation error; no changes are persisted.
- Employee does not own the request: System returns an authorization error.

---

### UC-005: Employee Submits a Draft Request

| Field | Value |
|-------|-------|
| **ID** | UC-005 |
| **Actor** | Employee (UR-01) |
| **Goal** | Forward a completed Draft request to the assigned manager for decision |
| **Trigger** | Employee selects "Submit" on a Draft request |
| **Preconditions** | Employee is authenticated; the request is in Draft state and owned by the employee |
| **Postconditions** | The request is in Submitted state; it is read-only for the employee; it appears in the assigned manager's review list |

**Main Flow:**
1. Employee selects "Submit" on a Draft request.
2. System validates that the request is in Draft state and is owned by the authenticated employee.
3. System transitions the request to Submitted state.
4. System makes the request visible to the assigned manager in the review list.

**Alternative Flows:**
- None.

**Exceptions:**
- Request is not in Draft state: System returns an error; no transition occurs.
- Employee does not own the request: System returns an authorization error.

---

### UC-006: Employee Cancels a Request

| Field | Value |
|-------|-------|
| **ID** | UC-006 |
| **Actor** | Employee (UR-01) |
| **Goal** | Withdraw a Draft or Submitted request |
| **Trigger** | Employee selects "Cancel" on a Draft or Submitted request |
| **Preconditions** | Employee is authenticated; the request is in Draft or Submitted state and owned by the employee |
| **Postconditions** | The request is in Cancelled state; no further transitions are permitted |

**Main Flow:**
1. Employee selects "Cancel" on their request.
2. System validates that the request is in Draft or Submitted state and is owned by the authenticated employee.
3. System transitions the request to Cancelled state.
4. System updates the employee's request list to reflect the Cancelled state.

**Alternative Flows:**
- None.

**Exceptions:**
- Request is in Approved, Rejected, or Cancelled state: System returns an error; no transition occurs.
- Employee does not own the request: System returns an authorization error.

---

### UC-007: Employee Views Request List and Final Decision

| Field | Value |
|-------|-------|
| **ID** | UC-007 |
| **Actor** | Employee (UR-01) |
| **Goal** | Check the current status of all personal requests and view final decisions |
| **Trigger** | Employee navigates to the request list screen |
| **Preconditions** | Employee is authenticated |
| **Postconditions** | Employee sees their complete request history including current state; for Approved or Rejected requests, the decision and optional manager comment are visible |

**Main Flow:**
1. Employee navigates to the request list.
2. System retrieves all requests owned by the authenticated employee.
3. System returns the list with current state for each request.
4. For Approved or Rejected requests, the final decision and manager comment (if any) are shown.

**Alternative Flows:**
- No requests exist: System returns an empty list.

**Exceptions:**
- None.

---

### UC-008: Manager Reviews Submitted Requests

| Field | Value |
|-------|-------|
| **ID** | UC-008 |
| **Actor** | Manager (UR-02) |
| **Goal** | See the list of Submitted requests awaiting a decision |
| **Trigger** | Manager navigates to the manager review list screen |
| **Preconditions** | Manager is authenticated |
| **Postconditions** | Manager sees all Submitted requests from employees assigned to them |

**Main Flow:**
1. Manager navigates to the review list.
2. System retrieves all Submitted requests from employees assigned to the authenticated manager (using the stored Manager reference on each Employee record per FR-AUTH-010 / BR-013).
3. System returns the list with request details (employee name, absence type, date range, reason).

**Alternative Flows:**
- No Submitted requests exist for assigned employees: System returns an empty list.

**Exceptions:**
- None.

---

### UC-009: Manager Approves a Submitted Request

| Field | Value |
|-------|-------|
| **ID** | UC-009 |
| **Actor** | Manager (UR-02) |
| **Goal** | Approve a Submitted request and record the decision |
| **Trigger** | Manager selects "Approve" on a Submitted request in the review list |
| **Preconditions** | Manager is authenticated; the request is in Submitted state; the request belongs to an employee assigned to the manager (per stored Manager reference on Employee record); the request is not owned by the manager |
| **Postconditions** | The request is in Approved state; exactly one Approval record exists with the authenticated manager as responsible approver, the decision, the optional comment, and the decision date |

**Main Flow:**
1. Manager selects "Approve" on a Submitted request.
2. Manager optionally enters a comment.
3. Manager confirms the action.
4. System validates: request is Submitted; the request belongs to an employee whose stored Manager reference matches the authenticated manager; the request is not the manager's own.
5. System transitions the request to Approved state.
6. System creates one Approval record using the authenticated manager's identity from the session.
7. System removes the request from the manager's pending review list.

**Alternative Flows:**
- None.

**Exceptions:**
- Request is not in Submitted state: System returns an error; no Approval record is created.
- Manager is not the assigned manager for the request's employee: System returns an authorization error.
- Manager owns the request: System returns an error; no Approval record is created.

---

### UC-010: Manager Rejects a Submitted Request

| Field | Value |
|-------|-------|
| **ID** | UC-010 |
| **Actor** | Manager (UR-02) |
| **Goal** | Reject a Submitted request and record the decision |
| **Trigger** | Manager selects "Reject" on a Submitted request in the review list |
| **Preconditions** | Manager is authenticated; the request is in Submitted state; the request belongs to an employee assigned to the manager (per stored Manager reference on Employee record); the request is not owned by the manager |
| **Postconditions** | The request is in Rejected state; exactly one Approval record exists with the authenticated manager as responsible approver, the decision, the optional comment, and the decision date |

**Main Flow:**
1. Manager selects "Reject" on a Submitted request.
2. Manager optionally enters a comment.
3. Manager confirms the action.
4. System validates: request is Submitted; the request belongs to an employee whose stored Manager reference matches the authenticated manager; the request is not the manager's own.
5. System transitions the request to Rejected state.
6. System creates one Approval record using the authenticated manager's identity from the session.
7. System removes the request from the manager's pending review list.

**Alternative Flows:**
- None.

**Exceptions:**
- Request is not in Submitted state: System returns an error; no Approval record is created.
- Manager is not the assigned manager for the request's employee: System returns an authorization error.
- Manager owns the request: System returns an error; no Approval record is created.

---

### UC-011: Manager Submits Their Own Absence Request

| Field | Value |
|-------|-------|
| **ID** | UC-011 |
| **Actor** | Manager (UR-02) acting as requester |
| **Goal** | Record a personal absence request as a requester, subject to the same employee-side rules |
| **Trigger** | Manager selects "New Request" on their own request screen |
| **Preconditions** | Manager is authenticated |
| **Postconditions** | A new Draft request exists owned by the Manager; the request is subject to the full employee-side lifecycle and cannot be self-approved |

**Main Flow:**
1. Manager opens the request creation form.
2. Manager selects an absence type, enters start date, end date, and reason.
3. Manager submits the form.
4. System validates date rules (start date not in the past; end date not before start date) — identical to Employee validation.
5. System persists the request in Draft state with the authenticated Manager as owner.
6. Manager may subsequently edit, submit, or cancel the Draft following the same rules as an Employee.

**Alternative Flows:**
- None.

**Exceptions:**
- Start date is in the past: System returns a validation error; no request is created.
- End date is before start date: System returns a validation error; no request is created.
- Required field missing: System returns a validation error; no request is created.

---

### UC-012: System Blocks Manager from Self-Approving

| Field | Value |
|-------|-------|
| **ID** | UC-012 |
| **Actor** | Manager (UR-02) |
| **Goal** | Ensure that the system enforces the self-approval prohibition when a Manager's own Submitted request is present in the approval workflow |
| **Trigger** | Manager navigates to the review list or attempts to approve or reject a request they own |
| **Preconditions** | Manager is authenticated; the Manager has a Submitted request of their own; the request appears in a context where an approval action could theoretically be attempted |
| **Postconditions** | No Approval record is created for the Manager's own request; the request remains in Submitted state; the system returns an error |

**Main Flow:**
1. Manager's own Submitted request exists in the system.
2. Manager attempts to approve or reject the request through any approval endpoint.
3. System reads the request owner from the database and compares it against the authenticated Manager's identity from the session.
4. System detects that the request owner and the authenticated approver are the same person.
5. System returns an error indicating self-approval is prohibited.
6. No Approval record is created; the request state is unchanged.

**Alternative Flows:**
- None.

**Exceptions:**
- None beyond the main blocking flow described above.

---

### UC-013: System Establishes Employee-Manager Assignment (Seed / Controlled Setup)

| Field | Value |
|-------|-------|
| **ID** | UC-013 |
| **Actor** | Developer / Test Administrator (UR-03) |
| **Goal** | Ensure each Employee record has exactly one assigned Manager reference before the acceptance demonstration |
| **Trigger** | Database seeding or controlled test-data setup at application startup or pre-demonstration configuration |
| **Preconditions** | At least one Manager account exists in the database (seeded per FR-AUTH-007); at least one Employee account exists or is being created |
| **Postconditions** | Each Employee record contains a stored reference to exactly one Manager; the reference is used by the system for all authorization checks in UC-008, UC-009, UC-010, and UC-012 |

**Main Flow:**
1. Developer / Test Administrator seeds or configures the database, creating or updating Employee records with a Manager reference.
2. System persists the Employee record with the assigned Manager reference.
3. System makes the Manager reference available for all approval routing and authorization decisions at runtime.

**Alternative Flows:**
- None.

**Exceptions:**
- Employee record is created without a Manager reference: The Manager review list (FR-APPR-001) will never show that employee's requests. This condition must be validated during setup — there is no runtime validation error or administrative UI to catch it after the fact.

---

## 5. Business Rules

| ID | Rule | Applies To | Source |
|----|------|------------|--------|
| BR-001 | The end date of a request cannot be earlier than the start date. | FR-EMP-007, FR-MGR-REQ-001 | SI-001 §5 Business Constraints |
| BR-002 | The start date of a request cannot be in the past. | FR-EMP-008, FR-MGR-REQ-001 | SI-001 §5 Business Constraints |
| BR-003 | Only Draft requests can be edited. | FR-EMP-002, FR-MGR-REQ-001 | SI-001 §4 In Scope |
| BR-004 | Only the request owner can edit, submit, or cancel their own request. | FR-EMP-002, FR-EMP-003, FR-EMP-004, FR-MGR-REQ-001 | SI-001 §5 Business Constraints |
| BR-005 | Only Submitted requests can be approved or rejected. | FR-APPR-007 | SI-001 §5 Business Constraints |
| BR-006 | Only the Manager assigned to the employee who owns the request can approve or reject that request. | FR-APPR-008 | SI-001 §5 Business Constraints |
| BR-007 | A Manager cannot approve or reject their own request. | FR-APPR-009, FR-MGR-REQ-002 | SI-001 §5 Business Constraints |
| BR-008 | Approving or rejecting a request must always create exactly one Approval record with the authenticated manager recorded as the responsible approver. | FR-APPR-005 | SI-001 §5 Business Constraints |
| BR-009 | Approved, Rejected, and Cancelled are final states — no further state transitions are permitted from these states. | FR-EMP-005, FR-APPR-007 | SI-001 §4 In Scope |
| BR-010 | The API always derives the current user identity and the responsible approver from the authenticated session; the frontend must never send a trusted employee or approver identifier for business decisions. | FR-AUTH-006, FR-APPR-006 | SI-001 §5 Technical Constraints |
| BR-011 | The absence type catalog (Vacation, Personal Leave, Sick Leave) is seeded and not user-maintainable; no administration screen is provided. | FR-AUTH-008 | SI-001 §4 In Scope |
| BR-012 | All business rules are enforced by the API, not only by the UI. | All FR entries | SI-001 §5 Technical Constraints |
| BR-013 | Each Employee record stores a single assigned Manager reference (one employee → one manager for the MVP), established via seed data or controlled setup — not through a self-service or administrative UI. This is a proposed resolution to the open gap in SI-001 §6 (Manager-to-employee assignment model), based on the "Delivery Architecture and Acceptance" meeting transcript, and is pending formal confirmation by James Parker (Sponsor). | FR-AUTH-010, FR-APPR-001, FR-APPR-008 | Meeting transcript "Delivery Architecture and Acceptance"; pending resolution of SI-001 §6 Gap |

---

## 6. Out of Scope

| ID | Feature | Reason for Exclusion |
|----|---------|----------------------|
| OS-001 | Microsoft Entra ID / corporate SSO | Adds significant complexity out of proportion with MVP goals; deferred pending decision to promote VacaFlow to a production system |
| OS-002 | Azure deployment and cloud hosting | MVP is local only; cloud deployment is a post-validation decision |
| OS-003 | Docker and CI/CD pipelines | Local execution from source code is sufficient for acceptance |
| OS-004 | Email and Microsoft Teams notifications | Users consult the application directly for status; deferred |
| OS-005 | Password reset and email verification flows | Manual database reset or seeded accounts are used during review; deferred |
| OS-006 | Account and role administration screens | Seeded data and controlled registration are sufficient for MVP |
| OS-007 | Vacation balance calculations | Requires policy decisions not in scope for this version |
| OS-008 | Holiday and working-day calendar calculations | Deferred; adds policy complexity |
| OS-009 | Overlapping request validation | Adds policy complexity deferred to a later phase |
| OS-010 | File attachments on requests | Deferred |
| OS-011 | Reports, exports, and dashboards | Deferred |
| OS-012 | HR administration views | Deferred |
| OS-013 | Multi-level approvals and approval delegation | Deferred |
| OS-014 | Integrations with payroll, HR, calendar, or directory systems | Deferred |
| OS-015 | Data migration from current email and spreadsheet records | Deferred |
| OS-016 | Advanced audit logs beyond the core Approval record | Deferred; Approval record satisfies MVP accountability requirement |
| OS-017 | Automated database backups | SQLite file location is documented; manual copy is sufficient for MVP |
| OS-018 | Absence type catalog maintenance screen | Catalog is seeded; no CRUD interface is provided in this version |
| OS-019 | Employee-to-Manager assignment UI or self-service assignment | Assignment is established via seed data or controlled setup only; no administrative or self-service interface is provided in this version |

---

## 7. External Integrations

| System | Purpose | Protocol | Notes |
|--------|---------|----------|-------|
| None | VacaFlow has no external system integrations in this version. All data is stored locally in SQLite. | N/A | All integrations (payroll, HR, calendar, directory, Teams) are explicitly excluded from scope per SI-001 §4 Out of Scope |

---

## 8. Traceability to SI

| FR ID | Requirement Summary | SI Section | SI Requirement |
|-------|---------------------|------------|----------------|
| FR-AUTH-001 | User registers with name, email, password, and role | SI-001 §4 In Scope | "Local application-managed registration and login (name, email, hashed password, role selection — Employee or Manager)" |
| FR-AUTH-002 | Passwords stored as hashes | SI-001 §5 Technical Constraints | "Passwords must be stored as hashes — plain text storage is a rejection condition" |
| FR-AUTH-003 | User can log in | SI-001 §4 In Scope | "Authentication: register, login, logout, current-user endpoints" |
| FR-AUTH-004 | User can log out | SI-001 §4 In Scope | "Authentication: register, login, logout, current-user endpoints" |
| FR-AUTH-005 | Current-user endpoint returns identity from session | SI-001 §5 Technical Constraints | "The API must derive the current user and responsible approver from the authenticated session; the frontend must never send a trusted employee or approver identifier" |
| FR-AUTH-006 | All business operations require valid authenticated session | SI-001 §5 Technical Constraints | Derived from the requirement that the API must enforce authenticated-session identity for all business operations |
| FR-AUTH-007 | At least one seeded Manager account | SI-001 §5 Technical Constraints | "Seeded data must include the three absence types and at least one manager account" |
| FR-AUTH-008 | Three absence types seeded on startup | SI-001 §4 In Scope | "Absence Type (Vacation, Personal Leave, Sick Leave — seeded)" |
| FR-AUTH-009 | Automatic database migration on startup | SI-001 §5 Technical Constraints | "The database must be generated or migrated automatically on startup" |
| FR-AUTH-010 | Employee record stores single Manager reference, set via seed/controlled setup | Meeting transcript "Delivery Architecture and Acceptance" | Proposed resolution of SI-001 §6 Gap: "Manager-to-employee assignment model not fully defined" — one manager per employee, established via seed data or controlled setup; pending James Parker confirmation |
| FR-EMP-001 | Employee creates Draft request | SI-001 §4 In Scope | "Employee actions: create a Draft request" |
| FR-EMP-002 | Employee edits Draft request | SI-001 §4 In Scope | "Employee actions: edit a Draft request" |
| FR-EMP-003 | Employee submits Draft request | SI-001 §4 In Scope | "Employee actions: submit a Draft request" |
| FR-EMP-004 | Employee cancels Draft or Submitted request | SI-001 §4 In Scope | "Employee actions: cancel a Draft or Submitted request" |
| FR-EMP-005 | Submitted/final-state requests are read-only for employee | SI-001 §4 In Scope | "Full request lifecycle: Draft, Submitted, Approved, Rejected, Cancelled — with all valid state transitions enforced by the API" |
| FR-EMP-006 | Employee views own request list and final decision | SI-001 §4 In Scope | "Employee actions: view own requests, view final decision" |
| FR-EMP-007 | End date cannot precede start date | SI-001 §5 Business Constraints | "Business rules enforced server-side: end date cannot precede start date" |
| FR-EMP-008 | Start date cannot be in the past | SI-001 §5 Business Constraints | "Business rules enforced server-side: start date cannot be in the past" |
| FR-MGR-REQ-001 | Manager creates, edits, submits, cancels own requests under Employee rules | SI-001 §4 In Scope | Manager role includes employee-level access for own requests; employee-side state machine and business rules apply equally |
| FR-MGR-REQ-002 | Manager cannot approve or reject own request | SI-001 §5 Business Constraints | "Business rules enforced server-side: a manager cannot approve or reject their own request" |
| FR-APPR-001 | Manager views Submitted requests from assigned employees | SI-001 §4 In Scope | "Manager actions: view Submitted requests assigned to them" |
| FR-APPR-002 | Manager approves Submitted request | SI-001 §4 In Scope | "Manager actions: approve a Submitted request with an optional comment" |
| FR-APPR-003 | Manager rejects Submitted request | SI-001 §4 In Scope | "Manager actions: approve or reject a Submitted request with an optional comment" |
| FR-APPR-004 | Manager includes optional comment with decision | SI-001 §4 In Scope | "Manager actions: approve or reject a Submitted request with an optional comment" |
| FR-APPR-005 | Exactly one Approval record per decision | SI-001 §5 Business Constraints | "Every approval or rejection creates one Approval record with the authenticated manager as responsible approver" |
| FR-APPR-006 | Approver identity derived from authenticated session | SI-001 §5 Technical Constraints | "The API must derive the … responsible approver from the authenticated session; the frontend must never send a trusted … approver identifier" |
| FR-APPR-007 | Only Submitted requests can be approved or rejected | SI-001 §5 Business Constraints | "Business rules enforced server-side: only the assigned Manager can approve or reject" (implying only Submitted state is eligible) |
| FR-APPR-008 | Manager can only decide on assigned employees' requests | SI-001 §5 Business Constraints | "Business rules enforced server-side: only the assigned Manager can approve or reject" |
| FR-APPR-009 | Manager cannot approve or reject own request | SI-001 §5 Business Constraints | "Business rules enforced server-side: a manager cannot approve or reject their own request" |

---

## 9. Document Control

### Review & Approval

| Role | Name | Date | Status | Comments |
|------|------|------|--------|----------|
| BSA | Laura Hernandez | | Pending | |
| Business Sponsor | James Parker | | Pending | Confirmation of BR-013 / FR-AUTH-010 (Employee-Manager assignment model) required |
| Tech Lead | | | Pending | |

### Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-07-18 | Laura Hernandez (AI Assisted) | Initial draft |
| 1.1 | 2026-07-18 | Laura Hernandez (AI Assisted) | Added FR-AUTH-010 (Employee-Manager assignment via seed/controlled setup); added BR-013; added UC-011 (now renumbered UC-013); updated FR-APPR-001 and FR-APPR-008 notes; updated §1.3 definition of Assigned Manager; updated FR-AUTH-001 notes; added OS-019; updated UC-008, UC-009, UC-010 to reference stored Manager reference; added UR-03 |
| 1.2 | 2026-07-18 | Laura Hernandez (AI Assisted) | Corrected SI section citations in Business Rules (§5 not §4 for constraints); corrected FR-AUTH-006 and FR-AUTH-010 traceability citations; changed FR-AUTH-010 / BR-013 source from SI-001 §4/§5 to meeting transcript with pending-confirmation language; added UC-011 (Manager Submits Own Absence Request) and UC-012 (System Blocks Manager from Self-Approving) to cover FR-MGR-REQ-001 and FR-MGR-REQ-002 use cases; renumbered former UC-011 to UC-013; replaced verbatim invented quote in FR-AUTH-006 traceability row with a paraphrase of the applicable SI-001 §5 Technical Constraints requirement |

---

## 10. Next Steps

- [ ] Review and sign this document — Owner: Laura Hernandez (BSA) — Target: 2026-07-25
- [ ] James Parker (Business Sponsor) to formally confirm the one-to-one Employee-to-Manager assignment model (BR-013 / FR-AUTH-010) as the resolved approach for the open gap in SI-001 §6, and to confirm business rules and scope alignment — Target: 2026-07-25
- [ ] Update SI-001 §5 Assumptions, §6 Gaps, and §7 Conditions to reflect the confirmed resolution of the Manager-to-employee assignment model once James Parker provides confirmation — Owner: Laura Hernandez — Target: 2026-07-25
- [ ] Technical Lead to validate feasibility of state machine, session-based identity enforcement, and seed-based Employee-Manager assignment model — Target: 2026-07-28
- [ ] Proceed to Phase 4: Requirements — Non-Functional Specification and Business Rules Catalog

**If Approved → Proceed to Phase 4:** Requirements — Non-Functional Specification (Target: 2026-07-28)

---
## Document Control

| Field | Value |
|-------|-------|
| Author | Laura Hernandez (AI Assisted) |
| Approval Authority | BSA |
| Status | Draft |
| Signature | ⏳ Pending — awaiting approval |

*— End of document —*
