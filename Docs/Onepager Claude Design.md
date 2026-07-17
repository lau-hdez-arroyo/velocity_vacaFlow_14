## Claude Design for Velocity — Meeting Recap

This document summarizes our working session on using Claude Design to build faithful, testable prototypes from our SDLC documents (Phases 01-03), aligned with the Velocity framework. Use it together with the accompanying NorteCarga practice

case to apply the approach hands-on.

## The 1-2-3 (Faithful Prototypes Built from SDLC Documents)

## 1. Prepare the ammunition: documents + look and feel

## Which documents to upload, and what each one is for:

- functional-spec.md — the heart of the prototype. User Roles & Personas, Functional Requirements, and Use Cases translate almost directly into screens and flows.

- strategic-intake.md — gives Claude the business context: Value Proposition, Stakeholders, Business Context. Without this, Claude designs the "what" without understanding the "who" or the "why."

- business-rules.md — brings the real rules (whichever categories apply to the project: user management, order processing, discounts, etc.) that make the prototype feel functional rather than just good-looking: validations, error messages, conditions.

- nonfunctional-spec.md — only the Usability section is worth passing along. Performance, Compliance, or Availability requirements are not tested in a visual prototype, so there is no need to load Claude with that weight.

- traceability-matrix.md — usually adds nothing to the design itself. Skip it at this stage unless you want Claude to explicitly cite which requirement each screen covers.

## On look and feel — since each project is new, there is no design system:

- 1. If the client has a brand manual, upload it as-is.

- 2. If not, request 2-3 screenshots of their website, current app, or any material carrying their identity. Claude can extract colors and tone from these. If the site is live, use Claude Design's web capture tool to pull it directly.

- 3. If none of the above exists, say so explicitly in the prompt: "This client has no brand manual; propose 2-3 professional, neutral visual directions based on their industry, and show them to me before applying one." This way Claude does not invent a visual identity without your knowledge or a chance to choose.

- 2. Have Claude interview you before designing (the tip that changes everything)

Instead of asking for the prototype right away, start with something like:

"Before designing anything, review these documents and ask me all the questions you need about roles, critical flows, and business rules that are unclear. Don't assume anything — I'd rather clarify now than correct later."

With specs this dense (multiple roles, many use cases, business rules), if you don't ask explicitly, Claude will fill the gaps on its own — and those silent assumptions become what the client sees and evaluates. Also ask Claude to summarize back the scope and flows it understood before it starts building, so you can correct course before it spends time on screens that don't serve the goal.

## 3. Ask for real functionality, not just pretty screens

- Be explicit: "I want a prototype that can be tested end to end, not just viewed" — complete flows (e.g., login → action → confirmation), not a single standalone screen.

- Work one use case at a time (from the Use Cases section of the functional spec). One prompt per flow produces better results than loading the entire document at once.

- Use the business rules to request real states: "when the user attempts [an action blocked by rule X], show them the corresponding error message."

- If there are multiple roles (User Roles & Personas), ask the prototype to simulate each role's view if that is part of what the client needs to validate.

## 4. The client refinement loop (before moving to development)


- When client feedback comes in, don't rewrite the prompt from scratch: use inline comments directly on the prototype, and ask Claude to tie the change back to the source document: "update this flow based on what the client requested, and tell me if it contradicts any rule in the business-rules.md I uploaded."

- Before moving to development, request two things: (a) the final exported version, and (b) a list of the differences between the approved prototype and the original spec, so the BSA can update the real functional-spec.md with what the client has already validated.

## Ready-to-Use Prompts

## 1. Kickoff prompt (documents + questions first):

"I'm attaching the functional-spec.md, strategic-intake.md, and business-rules.md for this project. Before designing anything, review all three and ask me any questions you need about roles, flows, and business rules that aren't clear. This client has no brand manual — I'm sharing 2 screenshots of their site for you to pull colors and visual tone from, or if you prefer, propose 2-3 neutral directions and we'll review them together."

## 2. Specific flow prompt (once Claude already knows the documents):

"Build the complete flow for use case [use case name] from the functional spec: screens, states, and validations according to the applicable rules in business-rules.md. I want to be able to test it end to end, not just view it."

## 3. Post-feedback refinement prompt:

"The client requested [feedback]. Update this flow accordingly and tell me if the change contradicts any rule in the business-rules.md I uploaded."

## Team Checklist

Identify which Phase 01-03 documents exist for the project (at minimum, functional-spec.md)

Resolve look and feel: brand manual / client screenshots / ask Claude to propose directions

ALWAYS start with the "ask questions before designing" prompt

Build one flow (use case) at a time, not the whole document at once

Before development: export the prototype + a list of differences vs. the original spec
