# VacaFlow Meeting Transcript 03 — Launch, Operations, and Handover

**Company:** IGS Solutions  
**Project:** VacaFlow  
**Date:** Tuesday, July 14, 2026  
**Time:** 11:00 AM – 12:10 PM  
**Approximate duration:** 70 minutes  
**Location:** Microsoft Teams  
**Participants:** Emily Harrison, Functional Analyst; James Parker, Operations Manager and project sponsor

## Meeting context

The meeting finalized how the limited MVP will be reviewed, what will be handed over, how the local application should be operated, and what will remain as future work.

## Transcript

**Emily Harrison:** Let us close the delivery expectations for this MVP. How should this version be delivered and reviewed?

**James Parker:** It should be delivered as source code that can be run locally. The reviewer should be able to start the API, start the web app, create or use accounts, and complete the request flow.

**Emily:** Should the database be included or generated?

**James:** Generated is fine, as long as the setup creates the absence types and a manager account. The instructions must explain how to reset the database if needed.

**Emily:** What does support mean for this limited MVP?

**James:** Support means fixing issues found during review, especially issues that block registration, login, request creation, submission, approval, rejection, or final status visibility. We are not asking for operational support after broad deployment because broad deployment is not part of this MVP.

**Emily:** Do we need a live support model with response times?

**James:** No. We need review support during the MVP validation window. Serious blocking defects should be addressed first. Cosmetic issues can wait.

**Emily:** What data must be protected in this version?

**James:** User emails, password hashes, names, request reasons, dates, and approval comments. We understand SQLite is local, but the database file should not be publicly exposed or committed with real passwords.

**Emily:** Should users be able to reset their passwords?

**James:** Not in the MVP. If someone forgets a password during review, we can reset the database or use a seeded account. Password reset is future work.

**Emily:** Should users receive email confirmation after registration?

**James:** No. That would require email setup and is out of scope.

**Emily:** Do we need a privacy notice or consent flow?

**James:** Not inside the MVP. We can document that the application stores basic employee identity and absence request data. If it becomes a production system, we will revisit privacy and retention formally.

**Emily:** How long should data be retained in the MVP?

**James:** No special retention rule. Data stays in the SQLite file until it is manually deleted or reset.

**Emily:** Do we need backups?

**James:** Not automated backups in the MVP. Since this is local, we just need to explain where the SQLite file is and how to copy it if needed.

**Emily:** Who signs off on this version?

**James:** I will sign off functionally. I may ask one manager and one employee to run the workflow before final acceptance.

**Emily:** What does acceptance mean?

**James:** The system is accepted when the full workflow works with registered accounts and the business rules hold. If someone can bypass the logged-in user identity or approve without being a manager, that is not acceptable.

**Emily:** What documentation do you expect?

**James:** A short README with setup instructions, how to run the API and web app, how to access SQLite, seeded accounts, endpoint summary, scope limitations, and the deferred backlog. Also the updated meeting transcripts and executive summary.

**Emily:** What is intentionally deferred?

**James:** Microsoft Entra ID or corporate single sign-on, Azure deployment, Docker, CI/CD, email notifications, password reset, account administration, vacation balances, holiday calendars, reports, HR views, overlapping request checks, attachments, and integrations.

**Emily:** If the MVP is successful, what is the most likely next step?

**James:** Decide whether to harden it for broader company use. That could include corporate sign-in, role administration, Azure hosting, backups, email notifications, and stronger audit records. But those are not part of this delivery.

**Emily:** What should we avoid while building this MVP?

**James:** Avoid turning it into an HR platform. Avoid adding optional features because they seem easy. Avoid building a complicated architecture. The value is the end-to-end workflow with real login, simple rules, and clear ownership.

## Decisions

- The MVP is delivered for local execution, not Azure deployment.
- SQLite remains the database for the MVP.
- Automated backups, password reset, email confirmation, and production support are out of scope.
- Acceptance depends on successful login, workflow execution, authorization, and rule enforcement.
- Future hardening may include Microsoft Entra ID or corporate single sign-on and Azure, but only in a later scope.

## Action items

- Emily will regenerate the executive summary with the confirmed MVP access model.
- Emily will regenerate the PowerPoint presentation to reflect the updated minimal MVP.
- James will validate the final acceptance checklist before implementation begins.
