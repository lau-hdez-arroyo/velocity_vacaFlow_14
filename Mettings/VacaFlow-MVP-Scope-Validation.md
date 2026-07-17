# VacaFlow MVP Scope Validation

**Company:** IGS Solutions  
**Project:** VacaFlow  
**Document purpose:** Validate the limited MVP scope with registration and login managed inside VacaFlow.  
**Date:** July 16, 2026

## 1. Validation outcome

The VacaFlow MVP is now defined as a deliberately limited internal application for managing employee vacation and absence requests. The MVP includes registration and login managed inside VacaFlow, while Microsoft Entra ID, corporate single sign-on, and external identity services remain out of scope.

The validated MVP is consistent with the current presentation direction, with one deliberate change: real local account registration and login are now included in the MVP instead of being excluded. All other advanced capabilities remain deferred unless explicitly approved later.

## 2. Confirmed MVP scope

### Included

- Next.js web application with a compact user experience.
- Basic local registration and login.
- Local user accounts with hashed passwords.
- Two application roles: Employee and Manager.
- Four business entities: Employee, Absence Type, Request, and Approval.
- SQLite database for application data and local authentication tables.
- ASP.NET Core Minimal API.
- A small Onion Architecture structure: Domain, Application, Infrastructure, API, and Web.
- Request workflow: Draft, Submitted, Approved, Rejected, and Cancelled.
- Explicit actions: create, edit Draft, submit, cancel, approve, and reject.
- Basic business rules for dates, editability, authorization, and approval responsibility.
- Seeded manager account and seeded absence types.
- Local execution from source code.

### Deferred

- Microsoft Entra ID or any corporate single sign-on provider.
- Azure deployment.
- Docker and CI/CD.
- Email or Teams notifications.
- Vacation balance calculations.
- Holiday calendars and working-day calculations.
- Request overlap validation.
- Attachments.
- Reporting, dashboards, exports, or formal document generation.
- HR administration screens.
- Multi-level approvals.
- Delegation of approval.
- Integration with payroll, HR, calendar, or directory systems.
- Data migration from existing systems.
- Advanced audit trail beyond the core approval record.

## 3. Authentication refinement

Corporate sign-in is not part of the MVP and remains deferred. The MVP uses application-managed registration and login.

The current MVP uses a local registration and login model:

- Users register with name, email, password, and role.
- Passwords are never stored in plain text.
- The API authenticates users before allowing request operations.
- The logged-in user identity is used to determine who owns a request.
- The logged-in manager identity is used as the responsible approver.
- The frontend no longer uses a simulated user selector.

For the limited MVP, Manager accounts may either be seeded or created through the same registration flow and reviewed manually during testing. No role administration screen is included.

## 4. Business entity boundary

The MVP still has four business entities:

1. **Employee** — the person represented in the vacation process, including role and basic profile information.
2. **Absence Type** — the catalog used to classify the request.
3. **Request** — the main workflow record with date range, reason, owner, and state.
4. **Approval** — the decision record with responsible manager, decision, comment, and date.

Authentication tables or account records are treated as technical infrastructure, not additional business entities for the MVP scope.

## 5. API boundary

The API now includes authentication endpoints plus the request workflow endpoints.

### Authentication

- Register.
- Login.
- Logout.
- Get current user.

### Catalog and workflow

- List absence types.
- List requests visible to the logged-in user.
- Create a Draft request.
- Edit a Draft request.
- Submit a request.
- Cancel a request.
- Approve a Submitted request.
- Reject a Submitted request.

The API must derive the current employee and responsible approver from the authenticated session or token. The frontend must not send a trusted employee or approver identifier for business decisions.

## 6. Web application boundary

The web MVP includes only the screens needed to complete the full workflow:

- Register.
- Login.
- Employee request list.
- Request form.
- Manager review list with approve and reject actions.

The application may use simple conditional rendering instead of a complex navigation structure. The UI should stay small and functional.

## 7. Rules validated for the MVP

- The end date cannot be earlier than the start date.
- The start date cannot be in the past.
- Only Draft requests can be edited.
- Only the owner can submit or cancel their request.
- Only Submitted requests can be approved or rejected.
- A manager cannot approve or reject their own request.
- Approval or rejection must create one approval record.
- The responsible approver must be the authenticated manager.
- Approved, Rejected, and Cancelled requests are final for the MVP.

## 8. Delivery implication

This scope is smaller than the previously validated operational version because it removes corporate identity, Azure deployment, and production hosting assumptions. The result is a self-contained MVP suitable for local execution and requirements validation.

If IGS Solutions later decides to deploy VacaFlow broadly, the deferred items should be revisited through a separate scope decision, especially authentication hardening, hosting, backups, role administration, and integration with corporate identity.

## 9. Final MVP statement

VacaFlow MVP will deliver a locally executable internal application where employees register, log in, create and submit absence requests, and managers log in to approve or reject them. The system will enforce the core workflow and rules using a minimal architecture, SQLite persistence, and a focused web interface.
