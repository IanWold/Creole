---
name: build
description: Implement a Creole card in the Build stage — plan against its acceptance criteria, get approval, then write code, tests and documentation together as discrete changes, keeping the card's progress notes current. Never commits.
argument-hint: "<card ID>"
---

# Build

Code and documentation are one stage. A Build increment is not done until its tests pass and `docs/` and XML docs reflect it (CONTRIBUTING.md § Documentation).

## Input

`$ARGUMENTS`: a card ID. If the card is `Not started`, run the `/start` checks first (blockers, entry stage). If it is `Researching`, stop. If it is `Designing`, stop until the maintainer has approved its decision record's options and criteria (`/decide`).

## Workflow

1. **Load context.** The card, its progress notes (resume from the last one), its artifacts, the linked whitepaper sections, accepted decisions for the area, and the code it builds on. A Proposed record the card owns is its working specification: where it differs from the whitepaper, it wins for this card. Read CLAUDE.md § Build and test for commands and conventions.
2. **Plan.** Map each acceptance criterion to the code, tests and docs that will satisfy it and how it will be verified. Note anything the whitepaper leaves ambiguous; each one is a small call you state in the plan, a revision of the card's Proposed record, or a new decision (`/decide`) if later work depends on it. If the card's record leaves options open, plan their trials first: which options, how each is run, and which of the record's criteria each result is read against. Split the work into discrete increments, each one reviewable on its own.
3. **Get approval.** Present the plan briefly and wait. Do not write code before the maintainer agrees.
4. **Try the open options**, if the card's record has any:
   - When two or more options need a trial, run them in parallel: one `general-purpose` subagent per option, with `isolation: "worktree"`. Brief each with the record's path, its option, the criteria, the commands that measure them, and the commit it starts from. Ask for results against the criteria, the diff, and problems found, not for a verdict on the other options.
   - Subagents build and test in parallel, but benchmarks run one option at a time, with the same job and inputs, so runs do not compete for the machine.
   - Read the results against the criteria yourself, record them with `/decide revise`, and present them to the maintainer. A trial that suggests a new option, or shows the criteria are wrong, is a revision to ask about, not a reason to widen the trial on your own.
   - Build the chosen option as ordinary increments in the main tree. Do not merge code from an option that was not chosen.
5. **Build each increment:**
   - Write tests alongside the code. Prefer tests that pin the semantics the whitepaper states (commit-by-default, rollback rules, span rules) over tests of implementation detail. Where the whitepaper calls for property-based, model-based or differential tests, write those.
   - Update `docs/` and XML docs in the same increment. A new `CRLnnnn` diagnostic gets its `docs/reference/diagnostics/CRLnnnn.md` page now.
   - Run the build and the relevant tests. Report failures honestly, with output.
   - Append a progress note to the card and offer a commit message (`<ID>: <imperative summary>`).
   - **Stop.** Do not start the next increment until the maintainer says they have committed this one. They review and edit before committing, so resume from what they committed: read `git show HEAD` (and `git status` for anything left uncommitted) before planning the next increment, and build on their version, not yours.
6. **If the design turns out wrong**, do not change the whitepaper's design or quietly deviate. If the card's Proposed record covers the question, revise it (`/decide revise`), tell the maintainer, and continue once they agree. Otherwise stop: set the card to `Designing`, write a progress note explaining what was found, and propose `/decide`.
7. **If scope grows**, finish what the card covers and propose a new card for the rest.
8. **When every criterion appears met** and each of the card's Proposed records has one option chosen, say so and stop. The maintainer opens the PR and runs `/complete` when they choose, which is where they accept the records.

Mechanical sub-steps (renames, moving types, call-site updates) go through `/refactor` as their own increment, never mixed with behaviour changes. Never run `git commit` or `git push`.

## Gotchas

- A trial's worktree is kept after its subagent ends if it changed anything. Once the chosen option is in the main tree, tell the maintainer which trial worktrees can be removed. The record keeps each trial's measurements and starting commit, not its code.
- The whitepaper's code samples are design sketches, not a spec of exact signatures. When an exact name or signature matters to later cards, settle it explicitly (in the plan, or with `/decide` if it is public API).
