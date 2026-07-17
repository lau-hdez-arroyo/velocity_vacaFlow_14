# VacaFlow MVP Executive Summary

**Company:** IGS Solutions  
**Project:** VacaFlow  
**Prepared for:** James Parker  
**Prepared by:** Emily Harrison  
**Date:** July 16, 2026

## 1. Executive overview

VacaFlow is a limited internal MVP for managing vacation and absence requests at IGS Solutions. The application will allow employees to register, log in, create and submit requests, and allow managers to log in to approve or reject submitted requests.

The current MVP uses registration and login with email and password managed inside VacaFlow. It does not use Microsoft Entra ID, Azure deployment, Docker, CI/CD, notifications, vacation balance calculation, holiday calendars, reports, attachments, or HR administration screens.

The intent is to validate the complete request lifecycle with the smallest practical scope.

## 2. Business problem

Vacation and absence requests are currently handled through informal channels such as email, chat, and spreadsheets. This creates uncertainty about request status, approval responsibility, and final decisions.

VacaFlow addresses this by creating one controlled request record with a clear lifecycle and a recorded manager decision.

## 3. MVP objectives

The MVP must demonstrate that IGS Solutions can manage the following workflow in a simple web application:

1. A user registers and logs in.
2. An employee creates a Draft request.
3. The employee edits the Draft if needed.
4. The employee submits the request.
5. The request becomes read-only for the employee.
6. A manager logs in.
7. The manager approves or rejects the Submitted request.
8. The system records the manager as the responsible approver.
9. The employee can view the final result.

## 4. Confirmed users

### Employee

The employee can:

- Register and log in.
- View their own requests.
- Create a request.
- Edit a Draft request.
- Submit a Draft request.
- Cancel a Draft or Submitted request.
- View the final decision.

### Manager

The manager can:

- Log in.
- View Submitted requests assigned to them.
- Approve a request.
- Reject a request.
- Add an optional decision comment.

No HR, administrator, external user, or executive dashboard role is included in the MVP.

## 5. Business entities

The MVP contains four business entities.

### Employee

Represents the person participating in the process. It stores name, email, role, active status, and simple manager assignment.

### Absence Type

Classifies the request. Initial types are Vacation, Personal Leave, and Sick Leave.

### Request

Represents the employee's absence request. It stores owner, absence type, date range, reason, current state, and relevant dates.

### Approval

Represents the manager's decision. It stores request, responsible manager, decision, optional comment, and decision date.

Authentication account data supports login but is treated as technical infrastructure rather than an additional business entity.

## 6. Request lifecycle

The request lifecycle is:

- Draft.
- Submitted.
- Approved.
- Rejected.
- Cancelled.

Valid transitions are:

- Draft to Submitted.
- Draft to Cancelled.
- Submitted to Approved.
- Submitted to Rejected.
- Submitted to Cancelled.

Approved, Rejected, and Cancelled are final states for the MVP.

## 7. Required business rules

The system must enforce the following rules:

- The end date cannot be earlier than the start date.
- The start date cannot be in the past.
- Only Draft requests can be edited.
- Only the request owner can edit, submit, or cancel their request.
- Only Submitted requests can be approved or rejected.
- Only a Manager can approve or reject.
- A manager cannot approve or reject their own request.
- The system must record the authenticated manager as the responsible approver.
- A request can have only one final decision.

## 8. Authentication scope

The MVP includes local registration and login:

- Register.
- Login.
- Logout.
- Current user profile.
- Hashed passwords.
- Employee and Manager roles.

The frontend must not send trusted employee or approver identifiers. The API must derive the current user from the authenticated session or token.

Deferred authentication features include:

- Microsoft Entra ID.
- Multifactor authentication.
- Password reset.
- Email verification.
- Account administration screens.
- External identity providers.

## 9. Technology scope

The MVP uses:

- Next.js and React for the web interface.
- ASP.NET Core Minimal API for backend processing.
- SQLite for persistence.
- Entity Framework Core for data access.
- A reduced Onion Architecture structure.

The application must run locally from source code.

## 10. Web application scope

The web application includes:

- Register screen.
- Login screen.
- Employee request list.
- Request creation and edit form.
- Manager review list.
- Approve and reject actions.

The MVP does not include dashboards, reports, account administration, HR views, custom branding, or complex navigation.

## 11. API scope

The API includes:

- Register.
- Login.
- Logout.
- Current user.
- List absence types.
- List visible requests.
- Create Draft request.
- Edit Draft request.
- Submit request.
- Cancel request.
- Approve request.
- Reject request.

## 12. Storage scope

SQLite is used for the MVP. The database file stores application data and local authentication data. The MVP does not include Azure SQL, cloud hosting, automated backup, or data migration.

## 13. Deferred backlog

The following items are explicitly deferred:

- Microsoft Entra ID.
- Azure deployment.
- Docker and CI/CD.
- Email or Teams notifications.
- Password reset and email verification.
- Account administration screen.
- Vacation balance calculation.
- Holiday calendars.
- Working-day calculations.
- Overlapping request validation.
- Attachments.
- HR views.
- Reports and exports.
- Multi-level approvals.
- Approval delegation.
- Payroll, HR, calendar, or directory integrations.
- Advanced audit logs.

## 14. Acceptance criteria

The MVP is accepted when IGS Solutions can demonstrate:

1. Registering a user.
2. Logging in.
3. Creating a Draft request.
4. Rejecting invalid date ranges.
5. Rejecting past start dates.
6. Editing a Draft request.
7. Submitting a request.
8. Preventing edits after submission.
9. Logging in as a manager.
10. Viewing Submitted requests assigned to the manager.
11. Approving or rejecting with a comment.
12. Recording the authenticated manager as responsible.
13. Showing the final decision to the employee.
14. Blocking unauthorized operations.

## 15. Final recommendation

The confirmed MVP should remain small: registration and login, four business entities, one request lifecycle, a focused web interface, Minimal API, SQLite, and local execution.

Any future move toward corporate sign-in, Azure hosting, notifications, HR administration, or broader operational use should be treated as a separate scope expansion.
