---
name: build
description: Implement a Creole card in the Build stage — plan against its acceptance criteria, get approval, then write code, tests and documentation together as discrete changes, keeping the card's progress notes current. Never commits.
argument-hint: "<card ID>"
---

# Build

Code and documentation are one stage. A Build increment is not done until its tests pass and `docs/` and XML docs reflect it (CONTRIBUTING.md § Documentation).

## Input

`$ARGUMENTS`: a card ID. If the card is `Not started`, run the `/start` checks first (blockers, entry stage). If it is `Researching` or `Designing`, stop: the design is not settled.

## Workflow

1. **Load context.** The card, its progress notes (resume from the last one), its artifacts, the linked whitepaper sections, accepted decisions for the area, and the code it builds on. Read CLAUDE.md § Build and test for commands and conventions.
2. **Plan.** Map each acceptance criterion to the code, tests and docs that will satisfy it and how it will be verified. Note anything the whitepaper leaves ambiguous; each one is either a small call you state in the plan or a decision (`/decide`) if later work depends on it. Split the work into discrete increments, each one reviewable on its own.
3. **Get approval.** Present the plan briefly and wait. Do not write code before the maintainer agrees.
4. **Build each increment:**
   - Write tests alongside the code. Prefer tests that pin the semantics the whitepaper states (commit-by-default, rollback rules, span rules) over tests of implementation detail. Where the whitepaper calls for property-based, model-based or differential tests, write those.
   - Update `docs/` and XML docs in the same increment. A new `CRLnnnn` diagnostic gets its `docs/reference/diagnostics/CRLnnnn.md` page now.
   - Run the build and the relevant tests. Report failures honestly, with output.
   - Append a progress note to the card and offer a commit message (`<ID>: <imperative summary>`).
5. **If the design turns out wrong**, stop. Do not change the whitepaper's design or quietly deviate. Set the card to `Designing`, write a progress note explaining what was found, and propose `/decide`.
6. **If scope grows**, finish what the card covers and propose a new card for the rest.
7. **When every criterion appears met**, hand off to `/complete`, which verifies them.

Mechanical sub-steps (renames, moving types, call-site updates) go through `/refactor` as their own increment, never mixed with behaviour changes. Never run `git commit` or `git push`.

## Gotchas

- The whitepaper's code samples are design sketches, not a spec of exact signatures. When an exact name or signature matters to later cards, settle it explicitly (in the plan, or with `/decide` if it is public API).
