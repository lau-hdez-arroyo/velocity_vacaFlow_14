# VacaFlow Meeting Transcript 01 — Product Scope and Workflow

**Company:** IGS Solutions  
**Project:** VacaFlow  
**Date:** Tuesday, July 7, 2026  
**Time:** 9:00 AM – 10:25 AM  
**Approximate duration:** 85 minutes  
**Location:** Microsoft Teams  
**Participants:** Emily Harrison, Functional Analyst; James Parker, Operations Manager and project sponsor

## Meeting context

The meeting focused on the business reason for VacaFlow, the limited MVP boundary, the users involved, the request lifecycle, and the core rules that must be enforced by the application.

## Transcript

**Emily Harrison:** James, before we discuss screens or implementation, I want to confirm the purpose of VacaFlow in your own words. What problem are we solving?

**James Parker:** We need a simple way for employees to request vacation or short absences and for managers to approve or reject them. Today, people use email, chat messages, and sometimes spreadsheets. The problem is not only the request itself; it is knowing whether it was sent, who approved it, and what the final decision was.

**Emily:** Is the objective to replace the full HR process?

**James:** No. This MVP is not a full HR platform. It is a controlled request workflow. We want to prove the basic flow first: employees create requests, managers decide, and the system keeps a clear record.

**Emily:** What name should we use for the project?

**James:** VacaFlow. It should be treated as the official project name for all documents and the application interface.

**Emily:** Why is this the right moment to implement it?

**James:** The number of informal requests has grown, and managers are spending too much time confirming status. We also had a recent case where an employee thought a request was approved because a manager replied informally in chat, but it was never recorded. That showed us we need a single place for these decisions.

**Emily:** Who are the users in the first version?

**James:** Only employees and managers. Employees request time off. Managers review and decide. We do not want an HR role in the MVP, and we do not want an administration module.

**Emily:** Let us define the user access model. Should this MVP use a corporate sign-in provider, or should access be handled inside VacaFlow?

**James:** For this MVP, access should be handled inside VacaFlow. Users should register and log in with an email and password managed by the application.

**Emily:** So users will create an account in VacaFlow with email and password?

**James:** Yes. A person registers with name, email, password, and role. We understand this is basic. We just need enough authentication to avoid the old user selector and make actions belong to the logged-in user.

**Emily:** Should passwords be stored directly?

**James:** No. Even in a small MVP, passwords must be stored securely as hashes. We do not need advanced corporate identity, but we do need basic security hygiene.

**Emily:** How should manager accounts be handled?

**James:** For the MVP, we can seed one manager account or allow a manager role during registration for testing. We do not need a role management screen. In the pilot, we can control who receives the manager login.

**Emily:** That keeps the scope small. Now let us talk about the core business objects. What does the system need to manage?

**James:** Employee, absence type, request, and approval. Employee identifies who is asking. Absence type classifies the request. Request is the main record. Approval records the manager decision.

**Emily:** Should authentication accounts be considered an additional business entity?

**James:** No. They are technical support for login. The business model should remain those four concepts.

**Emily:** What absence types should be available at the beginning?

**James:** Vacation, personal leave, and sick leave. We do not need a screen to maintain them. They can be loaded as seed data.

**Emily:** Walk me through the employee experience.

**James:** The employee opens the site, registers or logs in, sees their own requests, creates a new request, selects the absence type, start date, end date, and reason, then saves it as Draft. From there, the employee can edit or submit the request. Once submitted, the employee should not edit it.

**Emily:** Can the employee cancel it?

**James:** Yes, while it is Draft or Submitted. If the manager has already approved or rejected it, it is final for this MVP.

**Emily:** Walk me through the manager experience.

**James:** The manager logs in and sees Submitted requests. The manager opens or reviews each request, adds an optional comment, and approves or rejects. The system must record the manager who made the decision.

**Emily:** Should managers see all requests or only submitted ones?

**James:** For the MVP, only Submitted requests waiting for a decision. They do not need a full history screen. Employees can see their own request history.

**Emily:** Do we need to validate that a manager only approves requests from their own employees?

**James:** In the MVP we should have a simple manager assignment in the employee record. The API should check it. It should not rely on the frontend.

**Emily:** What states should the request support?

**James:** Draft, Submitted, Approved, Rejected, and Cancelled.

**Emily:** Are there any other states, such as returned for correction or pending HR review?

**James:** No. Those are useful later, but not in this MVP.

**Emily:** What are the must-have rules?

**James:** The end date cannot be before the start date. The start date cannot be in the past. Only Draft requests can be edited. Only Submitted requests can be approved or rejected. The responsible manager must be recorded when approving or rejecting. And an approved request cannot be modified.

**Emily:** Should the system calculate business days, holidays, or available balance?

**James:** No. We are not calculating balances. We are only recording calendar dates and decisions.

**Emily:** Should we prevent overlapping requests for the same employee?

**James:** Not in the MVP. That adds policy questions. We can list it as a future improvement.

**Emily:** Do users need attachments, supporting documents, or formal approval letters?

**James:** No. Not now.

**Emily:** Do you need notifications by email or Teams?

**James:** No. The first version can require users to check the application. Notifications would be helpful later but should not be in scope.

**Emily:** What does success look like for this MVP?

**James:** We can demonstrate the full cycle: register, login, create Draft, edit it, submit it, prevent editing after submission, log in as manager, approve or reject, and show the employee the final result with the manager recorded.

**Emily:** What would happen if this project did not move forward?

**James:** We would keep using emails and informal messages. That is workable but inconsistent. VacaFlow gives us a clear baseline and a better foundation for future improvements.

## Decisions

- VacaFlow remains the official project name.
- The MVP includes registration and login managed inside VacaFlow.
- The MVP keeps four business entities: Employee, Absence Type, Request, and Approval.
- The application will not include HR administration screens.
- The request states are Draft, Submitted, Approved, Rejected, and Cancelled.
- Vacation balance, holiday calendars, notifications, attachments, reports, and integrations are deferred.

## Action items

- Emily will keep corporate sign-in providers in the deferred scope and update the MVP package accordingly.
- Emily will define the API and UI boundaries using the basic login model.
- James will confirm the initial manager account and sample absence types.
