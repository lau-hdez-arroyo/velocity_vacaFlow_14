# VacaFlow Meeting Transcript 02 — Delivery, Architecture, and Acceptance

**Company:** IGS Solutions  
**Project:** VacaFlow  
**Date:** Friday, July 10, 2026  
**Time:** 2:00 PM – 3:35 PM  
**Approximate duration:** 95 minutes  
**Location:** Microsoft Teams  
**Participants:** Emily Harrison, Functional Analyst; James Parker, Operations Manager and project sponsor

## Meeting context

The meeting clarified the technical boundaries for the MVP, including the local authentication model, database choice, API surface, web application behavior, acceptance scenarios, and exclusions that must remain outside the current scope.

## Transcript

**Emily Harrison:** James, I want to confirm the architecture for the limited MVP. What should this version include technically?

**James Parker:** Next.js for the web interface, a .NET Minimal API for processing, SQLite for storage, and a simple layered structure. We still want Clean Architecture principles, but we do not want unnecessary patterns.

**Emily:** So a reduced Onion Architecture is acceptable?

**James:** Yes. The project should separate domain concepts, use cases, infrastructure, API endpoints, and the web interface. But we do not need MediatR, CQRS, generic repositories, messaging, or a large framework around it.

**Emily:** What should the authentication implementation look like?

**James:** Basic local registration and login. The user should register with name, email, password, and role. Login should validate the credentials and establish the current user. The API should know who is logged in when performing request actions.

**Emily:** Should login use a session cookie or token?

**James:** From a business perspective, we need a safe and simple authenticated session. The implementation team can choose the simplest consistent option, but passwords must be hashed and the current user must not be supplied by the frontend as a trusted business value.

**Emily:** That means create, submit, cancel, approve, and reject must use the authenticated user from the API context.

**James:** Correct. The UI can display the current user, but it cannot decide who owns the request or who approved it.

**Emily:** For manager approval, what check should exist?

**James:** A manager must have the Manager role and must be assigned as the manager of the employee who owns the request. Managers should not approve their own request.

**Emily:** Where is that assignment stored?

**James:** In the Employee record. This is not a full organization chart; it is a simple manager reference for the MVP.

**Emily:** Do we need account administration?

**James:** No. We can seed an initial manager account and allow basic registration for employees. If the test needs another manager, we can add it through seed data or controlled setup. No admin UI.

**Emily:** Let us discuss SQLite. Is it still the database for the MVP?

**James:** Yes. SQLite keeps the application self-contained. We are not deploying to Azure in this MVP. We need the application to run locally from source code.

**Emily:** Should we use migrations or automatic creation?

**James:** Use the simplest approach that is clear. If migrations are easy, use them; if not, create the database automatically for the MVP. What matters is that a reviewer can run the API and get the initial data.

**Emily:** What initial data do we need?

**James:** Absence types: Vacation, Personal Leave, and Sick Leave. Also at least one manager account so approvals can be tested. A sample employee account is useful but not required if registration is available.

**Emily:** What screens should the web app have?

**James:** Login, register, employee request list, request form, and manager review list. Keep it compact. It does not need a dashboard, reports, admin catalog management, or a design-heavy interface.

**Emily:** Should the same page change based on role, or should we have separate pages?

**James:** Either is fine. The important behavior is that employees see their own requests and managers see Submitted requests requiring decision. The interface should not show actions that are invalid for the current state.

**Emily:** Let us define the API surface. We now need authentication endpoints plus workflow endpoints.

**James:** Yes. Register, login, logout, and current user for authentication. Then absence types, requests, create, edit, submit, cancel, approve, and reject.

**Emily:** Should the employee ID be passed in the request body when creating a request?

**James:** No. The API should infer the employee from the authenticated user. That avoids someone creating a request for another employee.

**Emily:** Should the responsible manager ID be passed when approving?

**James:** No. Same idea. The API records the logged-in manager.

**Emily:** What should happen when the user tries an invalid action?

**James:** Return a clear error. For example, if someone tries to edit a Submitted request, the response should say only Draft requests can be edited. If a non-manager tries to approve, it should be forbidden.

**Emily:** What quality expectations matter most for this MVP?

**James:** Correctness, clarity, and reliability of the workflow. Speed is not a concern because the user count is small. Security matters enough that passwords must be protected and users cannot operate on requests they do not own.

**Emily:** Is high availability required?

**James:** No. This is a local MVP. We are not promising production availability.

**Emily:** Do we need accessibility standards?

**James:** Use basic readable forms and labels. We do not need a formal accessibility certification for the MVP, but the interface should not be difficult to use.

**Emily:** What acceptance scenarios must pass?

**James:** Register an employee, log in, create a Draft request, reject invalid dates, edit the Draft, submit it, verify it is no longer editable, log in as manager, approve or reject with a comment, record the manager, and let the employee see the final result.

**Emily:** Should we test rejected requests as well as approved ones?

**James:** Yes. Both decisions should create an approval record. The only difference is the final state and comment.

**Emily:** Do we need automated tests?

**James:** A few are enough. Unit tests for date rules and state transitions would be valuable. Full test automation is not required for the MVP.

**Emily:** What are the main risks?

**James:** The first risk is scope creep. The second is building authentication in a way that still allows users to spoof IDs from the frontend. The third is turning the interface into a larger HR tool. We must avoid all three.

## Decisions

- The MVP includes application-managed registration and login.
- The database remains SQLite.
- Azure deployment, Docker, and CI/CD are out of scope.
- Authentication endpoints are part of the MVP.
- Business workflow endpoints must derive user identity from authentication.
- No employee ID or approver ID should be trusted from the frontend for business decisions.
- The frontend includes only the screens required to complete the workflow.

## Action items

- Emily will update the MVP presentation to show registration and login as part of the included scope.
- Emily will update the API scope to include authentication endpoints.
- James will provide the initial manager account details and confirm whether role selection is allowed during registration for the MVP review.
