# Project Governance Framework: VacaFlow_14

**Author:** David Valdez (AI Assisted)
**Date:** 2026-07-21
**Version:** 1.0
**Status:** Draft
**Document ID:** GOV-001
**References:** SI-001 (Strategic Intake Document)

---

## Document Control

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-07-21 | David Valdez (AI Assisted) | Initial version |

---

## 1. Introduction

### 1.1 Purpose

This governance framework establishes the decision-making structures, authority boundaries, processes, and controls for VacaFlow_14 — the absence and vacation request management MVP for IGS Solutions. It defines who decides what, how changes are controlled, how issues are escalated, and what phase-gate criteria must be met before work proceeds from one SDLC phase to the next.

### 1.2 Scope

This framework applies to all aspects of the VacaFlow_14 project, including:

- Scope management and change control
- Decision-making authority and escalation
- Phase-gate review and acceptance
- Risk management
- Quality oversight
- Documentation and audit trail

### 1.3 Governance Objectives

- Provide clear, unambiguous decision authority at each level
- Enforce scope discipline on a tightly bounded MVP
- Ensure every phase transition has documented acceptance criteria and a named approver
- Maintain an auditable record of all decisions and change requests
- Operate without unnecessary bureaucratic overhead given the project's scale and training context

### 1.4 Governance Context

VacaFlow_14 is a bootcamp and training exercise for IGS Solutions, a fictional organization created for the exercise. The sponsor (James Parker), functional analyst (Emily Harrison), and BSA/Product Owner (Laura Hernandez) are illustrative personas. No live steering committee, external PMO, or corporate governance policy applies. Governance is therefore modeled as **direct phase-gate sign-off** rather than recurring committee oversight — a pattern already established in SI-001 §7 (Decision Framework) and §9 (Document Control).

---

## 2. Governance Structure

### 2.1 Organization Chart

```
┌──────────────────────────────────────────────────────────────┐
│                    SPONSOR PERSONA                           │
│              James Parker — Operations Manager               │
│         (Functional acceptance, scope boundary decisions,    │
│          final MVP sign-off after live demonstration)        │
└────────────────────────┬─────────────────────────────────────┘
                         │
          ┌──────────────┴──────────────┐
          │                             │
┌─────────┴──────────────┐   ┌──────────┴──────────────────┐
│   FUNCTIONAL ANALYST   │   │   TECHNICAL REVIEW          │
│   Emily Harrison       │   │   (Architecture / Stack)    │
│   (Artifact ownership, │   │   Delivery Team             │
│    scope decisions,    │   │   (Tech Lead, Dev, QA)      │
│    artifact currency)  │   └─────────────────────────────┘
└─────────┬──────────────┘
          │
┌─────────┴──────────────┐
│   BSA / PRODUCT OWNER  │
│   Laura Hernandez       │
│   (Functional spec,    │
│    requirements,        │
│    day-to-day scope)   │
└────────────────────────┘
```

### 2.2 Governance Bodies

Given the project's scale and training context, governance is handled through named roles with direct phase-gate authority rather than formal committee bodies.

#### Sponsor Persona — James Parker

| Aspect | Details |
|--------|---------|
| **Purpose** | Functional acceptance, scope boundary decisions, final MVP sign-off |
| **Authority** | Highest escalation authority; approves scope boundary changes and acceptance criteria |
| **Meeting cadence** | Phase-gate transitions (not recurring); ad hoc for critical issues |
| **Quorum** | N/A — sole authority at this level |

**Responsibilities:**
- Confirm manager-to-employee assignment model and all defined state transitions before Sprint 1 (per SI-001 §8)
- Confirm legal and regulatory applicability of storing employee identity and absence data (per SI-001 §7 Conditions to Proceed)
- Approve any changes that affect scope boundaries, business rules, or acceptance criteria (see §4)
- Provide functional sign-off at each SDLC phase transition from Understand through Architecture
- Conduct or delegate the live end-to-end acceptance demonstration
- Designate a backup artifact owner if Emily Harrison is unavailable during documentation or acceptance phases (per SI-001 §3 risk note)

---

#### Functional Analyst — Emily Harrison

| Aspect | Details |
|--------|---------|
| **Purpose** | Artifact ownership, requirement elicitation, scope decision documentation |
| **Authority** | Manages delivery artifacts; keeps decision records current |
| **Meeting cadence** | As required throughout the project |

**Responsibilities:**
- Elicit and document requirements, ensuring they reflect confirmed scope (SI-001 §4)
- Schedule and confirm the acceptance session date and participants (SI-001 §8)
- Verify local environment prerequisites with the development team before Sprint 1
- Maintain current ownership of all project artifacts
- Escalate artifact ownership gaps to James Parker immediately if unavailability arises

---

#### BSA / Product Owner — Laura Hernandez

| Aspect | Details |
|--------|---------|
| **Purpose** | Functional specification, requirements management, day-to-day scope decisions |
| **Authority** | Approves changes that do not affect core entities, state machine, or acceptance criteria |
| **Meeting cadence** | Continuous throughout development |

**Responsibilities:**
- Own and maintain the Functional Specification
- Approve minor changes within the defined two-tier change control model (see §4)
- Confirm acceptance criteria alignment with delivery team
- Coordinate with Emily Harrison on scope and artifact updates

---

#### Delivery Team

| Aspect | Details |
|--------|---------|
| **Purpose** | Technical design, development, and quality assurance |
| **Authority** | Technical decisions within the approved architecture (Clean Architecture / Onion, per NFR-MAINT-001/002) |
| **Meeting cadence** | Sprint ceremonies and daily coordination |

**Responsibilities:**
- Implement the approved scope as defined in SI-001 §4 and the Functional Specification
- Raise technical blockers or scope questions promptly for resolution
- Adhere to the approved technology stack: ASP.NET Core Minimal API, Next.js/React, EF Core, SQLite, Onion Architecture layers
- Ensure no real employee data is committed or exposed during review

---

## 3. Decision-Making Framework

### 3.1 Decision Culture

VacaFlow_14 operates under a **hierarchical** decision culture. Decision authority is not distributed by consensus — it is assigned to a named individual at each tier. Decisions escalate upward when they exceed the authority of the current tier.

### 3.2 Decision Authority Matrix

| Decision Type | BSA / Product Owner (Laura Hernandez) | Functional Analyst (Emily Harrison) | Sponsor Persona (James Parker) |
|---------------|:-------------------------------------:|:------------------------------------:|:-------------------------------:|
| Change to non-core entities or non-state-machine functionality | ✅ Approve | Consulted | Informed |
| Change affecting core entities (Employee, Absence Type, Request, Approval) | Consulted | Consulted | ✅ Approve |
| Change to request state machine | Consulted | Consulted | ✅ Approve |
| Change to acceptance criteria or demonstration script | Consulted | Consulted | ✅ Approve |
| Change to scope boundaries (In Scope / Out of Scope) | Consulted | Consulted | ✅ Approve |
| Technical architecture decisions (within approved stack) | Informed | Informed | Informed — Tech Lead decides |
| Phase-gate: proceed to next phase | Consulted | Reviewes artifacts | ✅ Approve |
| Blocking defect: defer vs. remediate before acceptance | Consulted | Consulted | ✅ Approve |
| Cosmetic defect: defer | ✅ Approve | Consulted | Informed |
| Acceptance session scheduling | Consulted | ✅ Approve | Confirms participation |

### 3.3 Decision-Making Process

```
┌────────────────────────────────────────────────────────────────┐
│                   DECISION-MAKING PROCESS                      │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  1. IDENTIFY the need for a decision                           │
│           │                                                    │
│           ▼                                                    │
│  2. CLASSIFY: does it affect core entities, state machine,     │
│     scope boundaries, or acceptance criteria?                  │
│           │                                                    │
│     ┌─────┴────────┐                                          │
│     │              │                                          │
│     ▼              ▼                                          │
│  NO — BSA/PO    YES — Escalate to Sponsor Persona             │
│  can approve    (James Parker)                                │
│     │              │                                          │
│     └──────┬────────┘                                         │
│            │                                                   │
│            ▼                                                   │
│  3. PRESENT options with recommendation and impact            │
│            │                                                   │
│            ▼                                                   │
│  4. DECIDE and record rationale in Decision Log               │
│            │                                                   │
│            ▼                                                   │
│  5. COMMUNICATE decision to delivery team and stakeholders    │
│            │                                                   │
│            ▼                                                   │
│  6. IMPLEMENT and track outcome                               │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 3.4 Decision Log

All decisions of record are logged in the following format:

| ID | Date | Decision | Options Considered | Choice | Rationale | Decided By |
|----|------|----------|--------------------|--------|-----------|------------|
| D001 | 2026-07-18 | Proceed with VacaFlow MVP with conditions | Proceed / Defer / Cancel | Proceed with Conditions | Real operational failure documented; scope well-bounded; conditions are information gaps, not blockers (per SI-001 §7) | James Parker (Sponsor) |

---

## 4. Change Management

### 4.1 Change Control Model

Given VacaFlow_14's fixed, tightly bounded MVP scope (SI-001 §4), change control uses a **two-tier model** without a formal change control board:

| Tier | Scope of Change | Approval Authority |
|------|-----------------|--------------------|
| **Tier 1 — Minor** | Changes that do not affect the four core entities (Employee, Absence Type, Request, Approval), the request state machine, or the acceptance demonstration script | BSA / Product Owner — Laura Hernandez |
| **Tier 2 — Scope-Impact** | Changes affecting scope boundaries, core entities, business rules enforced server-side, or acceptance criteria | Sponsor Persona — James Parker |

No formal project manager role exists, and no day-count or dollar-amount thresholds apply. The classification is based entirely on whether the change touches the MVP's protected scope perimeter.

### 4.2 Change Control Process

```
┌────────────────────────────────────────────────────────────────┐
│                  CHANGE CONTROL PROCESS                        │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  1. SUBMIT Change Request (see §4.3 template)                 │
│           │                                                    │
│           ▼                                                    │
│  2. ASSESS impact:                                            │
│     - Does it affect core entities, state machine,            │
│       scope boundaries, or acceptance criteria?               │
│           │                                                    │
│     ┌─────┴──────────┐                                        │
│     │                │                                        │
│     ▼                ▼                                        │
│   NO (Tier 1)    YES (Tier 2)                                 │
│   BSA/PO decides  Sponsor Persona decides                     │
│     │                │                                        │
│     └──────┬──────────┘                                       │
│            │                                                   │
│            ▼                                                   │
│  3. DECIDE: Approve / Reject / Defer                          │
│            │                                                   │
│     ┌──────┴──────┐                                           │
│  Approved       Rejected / Deferred                           │
│     │                │                                        │
│     ▼                ▼                                        │
│  4. UPDATE        Communicate outcome;                        │
│  scope baseline   document reason                             │
│  and delivery     in Decision Log                             │
│  artifacts                                                    │
│     │                                                         │
│     ▼                                                         │
│  5. IMPLEMENT change; update relevant SDLC documents         │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 4.3 Change Request Template

```
## Change Request

| Field        | Value                            |
|--------------|----------------------------------|
| CR ID        | CR-[XXX]                         |
| Title        | [Brief title]                    |
| Requestor    | [Name and role]                  |
| Date         | [YYYY-MM-DD]                     |
| Priority     | Low / Medium / High / Critical   |

### Description
[Detailed description of the requested change]

### Business Justification
[Why is this change needed? What operational or quality issue does it resolve?]

### Impact Assessment
| Area              | Impact         | Details                          |
|-------------------|----------------|----------------------------------|
| Core entities     | [Yes / No]     | [Which entities affected]        |
| State machine     | [Yes / No]     | [Which transitions affected]     |
| Acceptance script | [Yes / No]     | [Which steps affected]           |
| Scope boundary    | [Yes / No]     | [In Scope / Out of Scope change] |
| Delivery timeline | [X] days       | [Which tasks affected]           |
| Risk              | [Low/Med/High] | [New risks introduced]           |

### Classification
[ ] Tier 1 — Minor (BSA/PO approval)
[ ] Tier 2 — Scope-Impact (Sponsor approval)

### Recommendation
[Recommending party's recommendation: Approve / Reject / Defer with rationale]

### Decision
| Decided By    | Decision  | Date       | Conditions / Notes |
|---------------|-----------|------------|--------------------|
| [Authority]   | [A/R/D]   | [YYYY-MM-DD] | [Any conditions] |
```

### 4.4 Change Classification Reference

| Classification | Criteria | Authority |
|----------------|----------|-----------|
| **Tier 1 — Minor** | No effect on core entities, state machine, or acceptance demonstration | Laura Hernandez (BSA/PO) |
| **Tier 2 — Scope-Impact** | Affects core entities (Employee, Absence Type, Request, Approval), request state transitions, enforced business rules, scope boundaries, or acceptance criteria | James Parker (Sponsor Persona) |

---

## 5. RACI Matrix

### 5.1 Project Activities RACI

| Activity | James Parker (Sponsor) | Emily Harrison (Functional Analyst) | Laura Hernandez (BSA/PO) | Delivery Team |
|----------|:----------------------:|:------------------------------------:|:------------------------:|:-------------:|
| Strategic Intake (SI-001) | A | R | R | I |
| Functional Specification | C | C | A/R | C |
| Non-Functional Requirements | C | C | A | R |
| Business Rules | C | C | A | R |
| Traceability Matrix | I | C | A | R |
| Architecture Design | C | I | C | A/R |
| Sprint Backlog and Roadmap | I | C | A | R |
| Development | I | I | C | A/R |
| Testing and QA | C | C | C | A/R |
| Live Acceptance Demonstration | A | R | R | R |
| Go/No-Go Decision (Phase Gates) | A | R | R | I |
| Scope Change — Tier 1 | I | C | A/R | I |
| Scope Change — Tier 2 | A | C | R | I |
| Risk Identification | C | R | A | R |
| Risk Response | A | R | R | R |
| Issue Escalation | A | R | R | C |
| Documentation Currency | I | A/R | C | C |
| Blocking Defect Decision | A | C | C | R |
| Cosmetic Defect Deferral | I | C | A | R |

**Legend:** R = Responsible, A = Accountable, C = Consulted, I = Informed

### 5.2 Deliverable Approval RACI

| Deliverable | Creator | Reviewer | Approver |
|-------------|---------|----------|----------|
| Strategic Intake (SI-001) | Laura Hernandez / Emily Harrison | James Parker | James Parker (Sponsor) |
| Functional Specification | Laura Hernandez | Emily Harrison, Delivery Team | Laura Hernandez (BSA/PO) |
| Non-Functional Requirements | Delivery Team / BSA | Emily Harrison | Laura Hernandez (BSA/PO) |
| Architecture Design (SAD) | Tech Lead / Architect | BSA/PO | Solution Architect |
| Backlog and Roadmap | Delivery Team | BSA/PO, Emily Harrison | Laura Hernandez (BSA/PO) |
| Test Plan and Test Cases | QA | BSA/PO | Laura Hernandez (BSA/PO) |
| Acceptance Demonstration | Delivery Team | Emily Harrison, James Parker | James Parker (Sponsor) |
| Governance Framework (GOV-001) | David Valdez | All roles | David Valdez (Executive Sponsor) |

---

## 6. Phase-Gate Reviews

### 6.1 Phase Gate Definitions

Given the hierarchical, phase-gated oversight model established in SI-001, each SDLC phase requires explicit sign-off before the next phase begins. No phase may start without the prior phase gate being passed.

| Gate | Phase Completed | Review Authority | Go / No-Go Criteria |
|------|-----------------|------------------|----------------------|
| G1 | Understand (Strategic Intake) | James Parker (Sponsor) | Strategic Intake signed; scope boundaries confirmed; Conditions to Proceed accepted (SI-001 §7) |
| G2 | Define (Functional Specification) | James Parker (Sponsor) | Functional Specification signed; use cases and acceptance criteria confirmed; manager-to-employee assignment model resolved |
| G3 | Requirements | James Parker (Sponsor) | NFR, Business Rules, and Traceability Matrix signed; regulatory applicability confirmed |
| G4 | Architecture | James Parker (Sponsor) | Software Architecture Document signed; stack and Onion layer structure confirmed; no blocking technical open items |
| G5 | Development (Code Complete) | Laura Hernandez (BSA/PO) | All in-scope features implemented; unit tests passing; no plain-text passwords; session-derived identity enforced; database auto-migrates on startup |
| G5b | Quality | Laura Hernandez (BSA/PO) | Test cases executed; exit criteria met; UAT script ready for acceptance demonstration |
| G6 | Acceptance Demonstration | James Parker (Sponsor) | Live end-to-end demonstration completed successfully per the script defined in SI-001 §5; no blocking defects; any cosmetic issues formally deferred |

### 6.2 Acceptance Demonstration Script (Gate G6)

Per SI-001 §5 (Business Constraints), final acceptance requires a live end-to-end demonstration with real registered accounts covering the complete workflow in the following sequence:

1. Register a new employee account
2. Log in as the employee
3. Create a Draft request; validate date rules (end date ≥ start date; start date not in the past)
4. Edit the Draft request
5. Submit the Draft request; confirm it is no longer editable
6. Create a second Draft request; cancel it; confirm Cancelled state is displayed correctly
7. Log in as the assigned manager; view only Submitted requests pending decision
8. Approve or reject the Submitted request with an optional comment
9. Confirm the manager's identity is recorded as the responsible approver on the Approval record
10. Log back in as the employee; view the final decision and comment
11. Confirm that a manager cannot approve or reject their own request

Any bypass of authenticated-user identity or approval by a non-manager during this demonstration is a rejection condition. Blocking defects found during the review window must be remediated before final acceptance. Cosmetic issues may be formally deferred by James Parker at his discretion.

### 6.3 Gate Review Process

1. **Prepare**: Compile gate review package — signed documents, outstanding items, demo environment checklist
2. **Present**: Walk through deliverables with the gate authority
3. **Assess**: Evaluate against the Go / No-Go criteria in §6.1
4. **Decide**: Go / Conditional Go / No-Go — record in Decision Log (§3.4)
5. **Document**: Record the decision, conditions, and owner of any remediation items
6. **Proceed**: Begin the next phase, or remediate and re-present

---

## 7. Reporting and Oversight

### 7.1 Reporting Schedule

Given the project's scale (bootcamp exercise, no formal PM role), reporting is lightweight and focused on artifacts rather than recurring status meetings.

| Report / Artifact | Audience | Frequency | Owner |
|-------------------|----------|-----------|-------|
| SDLC Document Status | All roles | Per phase completion | Laura Hernandez (BSA/PO) |
| Decision Log updates | James Parker, Emily Harrison | As decisions are made | Laura Hernandez / Emily Harrison |
| Change Request status | Relevant tier authority | Per submission | Requestor |
| Risk Register | All roles | At each phase gate | Delivery Team / BSA |
| Acceptance Demonstration readiness | James Parker | At Gate G6 scheduling | Emily Harrison |
| Issue / Blocker log | All roles | As issues arise | Delivery Team |

### 7.2 Key Performance Indicators

| KPI | Target | Measurement |
|-----|--------|-------------|
| Phase-gate completion rate | 100% of gates passed before phase proceeds | Phase-gate log |
| Scope change Tier 2 (sponsor-level) volume | Minimize; target 0 unplanned scope-boundary changes | Change Request log |
| Blocking defects at acceptance | 0 blocking defects at Gate G6 | Defect log / UAT report |
| Acceptance demonstration pass | Complete script executed without blocking issues | Live demonstration record |
| Open conditions from SI-001 resolved before Sprint 1 | All 3 conditions resolved (manager assignment model, legal/regulatory applicability, acceptance session scheduled) | Decision Log; SI-001 §7 |

---

## 8. Issue and Escalation Management

### 8.1 Issue Severity Classification

| Severity | Definition | Response Time | Escalation |
|----------|------------|---------------|------------|
| **Blocking** | Project cannot proceed; acceptance at risk; confirmed technical impossibility within scope | Same day | Immediate to James Parker (Sponsor) |
| **High** | Significant scope or quality impact; risk of missing acceptance criteria | 1 business day | James Parker (Sponsor) |
| **Medium** | Moderate impact; manageable with known mitigation | 3 business days | Laura Hernandez (BSA/PO) |
| **Low** | Minor quality or process issue; no impact on acceptance criteria | 1 week | Delivery Team lead |

### 8.2 Escalation Path

```
              ┌─────────────────────────────┐
              │   SPONSOR PERSONA           │
              │   James Parker              │
              │   (Blocking / High issues,  │
              │    scope and acceptance)    │
              └──────────────┬──────────────┘
                             │ Same day (Blocking) / 1 day (High)
              ┌──────────────┴──────────────┐
              │   FUNCTIONAL ANALYST        │
              │   Emily Harrison            │
              │   (Artifact and scope       │
              │    decision escalation)     │
              └──────────────┬──────────────┘
                             │ 3 days (Medium)
              ┌──────────────┴──────────────┐
              │   BSA / PRODUCT OWNER       │
              │   Laura Hernandez           │
              │   (Day-to-day scope and     │
              │    requirements decisions)  │
              └──────────────┬──────────────┘
                             │ 1 week (Low)
              ┌──────────────┴──────────────┐
              │   DELIVERY TEAM             │
              │   (Technical issues,        │
              │    implementation decisions)│
              └─────────────────────────────┘
```

### 8.3 Known Risks Requiring Governance Oversight

The following risks identified in SI-001 require active governance monitoring:

| Risk | Governance Response | Owner |
|------|---------------------|-------|
| Emily Harrison unavailable during documentation or acceptance phase | James Parker must designate a backup artifact owner before kickoff | James Parker |
| Manager-to-employee assignment model not confirmed before Sprint 1 | Gate G1 condition — Sprint 1 cannot begin without resolution | James Parker |
| Acceptance session not scheduled before Phase 2 approval | Gate G2 condition — functional spec sign-off requires confirmed session date | Emily Harrison |
| Local environment prerequisites not validated before Sprint 1 | Delivery Team lead to confirm with Emily Harrison; blocker if unresolved | Emily Harrison |
| SQLite concurrency insufficient for review population | James Parker to confirm review participants are five or fewer concurrent users | James Parker |

---

## 9. Compliance and Regulatory Context

### 9.1 Regulatory Applicability

As documented in SI-001 §5 (Legal Constraints) and reflected in NFR-COMP-001 and NFR-COMP-002:

- No GDPR, HIPAA, SOC 2, or equivalent regulatory framework has been confirmed as applicable to this MVP in its current training/bootcamp context
- IGS Solutions, James Parker, and Emily Harrison are illustrative constructs; no real employee data is collected or stored in this exercise
- Confirmation of regulatory applicability for any future production use is a **Condition to Proceed** (SI-001 §7), owned by James Parker, with a target date of 2026-07-25

**Governing constraint:** If VacaFlow is promoted to a production system, privacy policy, data retention rules, and formal compliance requirements must be assessed as a separate scope decision before promotion — this is not in scope for this MVP.

### 9.2 Technical Compliance Controls

The following controls are enforced by design, not by policy review:

| Control | Enforcement Mechanism |
|---------|----------------------|
| No plain-text passwords | Passwords stored as hashes; plain-text storage is a Gate G6 rejection condition |
| Session-derived identity for approvals | API derives identity from authenticated session; frontend must not send trusted identifiers |
| No public database exposure | Database file must not be committed with real passwords or exposed publicly |
| Role enforcement | Manager role required to approve or reject; enforced server-side |
| Self-approval prevention | A manager cannot approve or reject their own request; enforced server-side |

### 9.3 Audit Trail

All governance decisions, change requests, and phase-gate outcomes are recorded in:

- **Decision Log** (§3.4) — all decisions with rationale and authority
- **Change Request Log** — all submitted changes with classification and outcome
- **Phase-Gate Log** — all gate reviews with Go / No-Go records and conditions
- **SDLC Document audit trail** — all signed documents with signer identity and timestamp

The Approval record within the VacaFlow application itself (one record per approval/rejection with the authenticated manager as responsible approver) also forms part of the functional audit trail for the MVP's business workflow.

---

## 10. Document Management

### 10.1 Document Ownership and Review

| Document | Owner | Review Trigger | Approval |
|----------|-------|----------------|----------|
| Strategic Intake (SI-001) | Laura Hernandez | Phase-gate G1 | James Parker |
| Governance Framework (GOV-001) | David Valdez | Phase-gate G1 | David Valdez (Executive Sponsor) |
| Functional Specification | Laura Hernandez | Phase-gate G2 | Laura Hernandez (BSA/PO) |
| Non-Functional Requirements | Laura Hernandez | Phase-gate G3 | Solution Architect |
| Business Rules | Laura Hernandez | Phase-gate G3 | Solution Architect |
| Traceability Matrix | Laura Hernandez | Phase-gate G3 | Solution Architect |
| Architecture Documents | Delivery Team | Phase-gate G4 | Solution Architect |
| Backlog and Roadmap | Delivery Team | Phase-gate G5 | Laura Hernandez (BSA/PO) |
| Test Plan, Test Cases | QA | Phase-gate G5b | Laura Hernandez (BSA/PO) |
| Change Requests | Requestor | Per submission | Per classification (§4.1) |
| Risk Register | Delivery Team / BSA | Per phase gate | Laura Hernandez (BSA/PO) |

### 10.2 Version Control

- All project documents are version controlled using the SDLC document management system
- Major changes result in a full version increment (1.0 → 2.0)
- Minor changes result in a decimal increment (1.0 → 1.1)
- All changes are logged in each document's version history table
- No document may be regenerated or modified without the change being traceable to a Decision Log entry or Change Request

### 10.3 Alignment with Technical Standards

The one technical standard this project aligns to at the organizational level is the Clean Architecture / Onion reference used across IGS Solutions bootcamp projects (`Docs/Clean-Architecture-Onion-DotNet-Reference.md`). VacaFlow's NFR-MAINT-001 and NFR-MAINT-002 commit the delivery team to this standard. The Delivery Team is accountable for compliance; any deviation requires Tier 1 or Tier 2 change control depending on impact.

---

## Approval

| Role | Name | Date | Status |
|------|------|------|--------|
| Executive Sponsor | David Valdez | Pending | Pending |
| BSA / Product Owner | Laura Hernandez | Pending | Pending |
| Functional Analyst | Emily Harrison | Pending | Pending |

---
## Document Control

| Field | Value |
|-------|-------|
| Author | David Valdez (AI Assisted) |
| Approval Authority | Executive Sponsor |
| Status | Draft |
| Signature | ⏳ Pending — awaiting approval |

*— End of document —*
