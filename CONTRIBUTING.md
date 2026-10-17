# Contributing to Creole

This document describes how work moves through the Creole project: where things live, what each stage produces, and what "done" means. It applies to human and AI contributors alike. [CLAUDE.md](CLAUDE.md) adds the rules specific to Claude sessions.

## Contents

- [Overview](#overview)
- [Repository layout](#repository-layout)
- [Sources of truth](#sources-of-truth)
- [The lifecycle](#the-lifecycle)
- [Cards](#cards)
- [Epics, phases, waves and gates](#epics-phases-waves-and-gates)
- [Research notes](#research-notes)
- [Decision records](#decision-records)
- [Ideas](#ideas)
- [Documentation](#documentation)
- [Changing the whitepaper](#changing-the-whitepaper)
- [Discrete changes and commits](#discrete-changes-and-commits)
- [Completing a card](#completing-a-card)
- [Completing an epic](#completing-an-epic)
- [Keeping the tracker consistent](#keeping-the-tracker-consistent)
- [Working with Claude](#working-with-claude)

## Overview

The [whitepaper](whitepaper.md) describes the system Creole intends to be. The [work tracker](work/README.md) breaks it into epics and cards. Most cards were split out of the whitepaper; new ones come from ideas, research findings, decisions, or follow-up work found while building.

Each card moves through some or all of **Research → Design → Build**, and every stage leaves an artifact behind: a research note, a decision record, or code with its documentation. A card can enter at any stage. A card that only needs code starts at Build; a card whose approach the whitepaper already settles skips Research; a spike may end at Design with a decision and no code. [The lifecycle](#the-lifecycle) gives the full detail.

## Repository layout

| Path | Contents |
| --- | --- |
| [`whitepaper.md`](whitepaper.md) | The design. A living specification: it always describes the intended system. |
| [`docs/`](docs/README.md) | User-facing documentation of the system as built. Ships with the library. |
| [`work/README.md`](work/README.md) | Roadmap, phases, waves, critical path, in-progress and ready lists, and the epic tables. |
| `work/active/<NN-epic>/` | Cards for epics with open work. |
| `work/complete/<NN-epic>/` | Epics whose cards are all Done. |
| `work/backburner/` | Parked epics or cards (created when first needed). |
| `work/ideas/` | Ideas not yet ready to be cards (created when first needed). |
| [`work/docs/research/`](work/docs/research/README.md) | Research notes: what we learned and from where. |
| [`work/docs/decisions/`](work/docs/decisions/README.md) | Decision records: what we chose and why. |
| [`work/docs/archive/`](work/docs/archive/README.md) | Superseded process artifacts kept for history. |
| [`scripts/work.py`](scripts/work.py) | Tracker checks and mechanical edits (see [Keeping the tracker consistent](#keeping-the-tracker-consistent)). |
| `.claude/` | Claude Code configuration: skills, subagents, path-scoped rules and hooks (see [Working with Claude](#working-with-claude)). |

Source code, tests, benchmarks and samples will be laid out by [FND-02](work/active/01-foundations/FND-02-repository-and-solution-scaffolding.md).

`docs/` and `work/docs/` are deliberately separate. `docs/` is the product: it describes Creole for its users and must be true of the current code. `work/docs/` is the process: it records how the project got here and is never shipped.

## Sources of truth

When two sources disagree, the higher one wins until the conflict is resolved. A conflict is a defect: flag it, do not silently pick one.

1. **Accepted decision records** in `work/docs/decisions/`, for the question each one decides.
2. **The whitepaper**, for intended design. Accepted decisions are folded into it (see [Changing the whitepaper](#changing-the-whitepaper)), so a conflict between 1 and 2 means a whitepaper update was missed.
3. **Cards**, for scope, acceptance criteria and dependencies. A card that contradicts the whitepaper is wrong, or needs a decision.
4. **`docs/` and the code**, for behaviour as built. Where they differ from the whitepaper, either the code has a bug or the design changed without a decision. Both need fixing.

## The lifecycle

| Stage | Card Status | Purpose | Artifact | Ends when |
| --- | --- | --- | --- | --- |
| Idea | — (not a card yet) | Capture a thought before it is lost. | `work/ideas/<slug>.md` | Promoted to a card or research note, or dropped. |
| Research | `Researching` | Find out what is known: prior art, papers, other libraries, measurements, .NET constraints. | Research note | The question is answered well enough to design, with sources. |
| Design | `Designing` | Choose an approach and record why. | Decision record; whitepaper and card updates | The decision is Accepted by the maintainer. |
| Build | `Building` | Code **and** documentation, together, with tests. | Code, tests, `docs/` pages | Every acceptance criterion is met and checked off. |
| Done | `Done` | — | `## Outcome` section on the card | — |

`Not started` is the status of a card nobody has picked up. There is no separate Documentation stage: a Build change that alters public behaviour without updating `docs/` is not finished.

**Choosing the entry stage.** When a card is started, decide which stage it needs first:

- **Research** if the card asks a question, names prior art to study, measures something, or the whitepaper marks the area as open (§ Risks and open questions).
- **Design** if the approach is unclear but no outside knowledge is needed, or the card ends in a decision (for example [FND-07](work/active/01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md), [GLL-05](work/active/18-general-parsing-spike/GLL-05-general-parsing-decision.md)).
- **Build** if the whitepaper and card already pin down what to build.

A card may move backwards: Build can uncover a design question, and the card returns to `Designing` until a decision exists.

## Cards

A card is one Markdown file, `work/<location>/<NN-epic>/<PREFIX>-<n>-<slug>.md`. Its ID (for example `CORE-04`) never changes and is never reused.

### Format

```markdown
# CORE-04: Sequencing and mapping nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Size | M |
| Status | Not started |
| Gate | Phase 0 gate |            ← only on gate cards

## Summary
One or two sentences.

## Scope
- Bullet list of what is in. Say what is out if it is easy to assume otherwise.

## Acceptance criteria
- [ ] Checkable statements. Each one must be verifiable by test, measurement or inspection.

## Blocked by
- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)
  (or "Nothing. This item can start immediately.")

## Unblocks
- [INT-03 …](../03-interpreter/INT-03-….md)
  (or "Nothing directly.")

## Whitepaper references
- [§ Section name](../../../whitepaper.md#anchor)

## Artifacts                    ← added as they are produced
- Research: [2026-10-20 Pidgin's sequencing internals](../../docs/research/2026-10-20-pidgin-sequencing.md)
- Decision: [ADR-0003 Map fusion in the node model](../../docs/decisions/0003-map-fusion.md)
- Docs: [Sequencing](../../../docs/concepts/sequencing.md)

## Progress notes               ← added when work starts; append-only
- 2026-10-21 (Building): Seq and Map nodes in place; Then/Before pending. Next: …

## Outcome                      ← added when Done
What was delivered and where, deviations from the card or whitepaper (with decision links), and any follow-up cards created.
```

The first seven sections are fixed and appear on every card. `Artifacts`, `Progress notes` and `Outcome` appear when there is something to put in them.

### Status values

`Not started` · `Researching` · `Designing` · `Building` · `Done`. Nothing else. Parked cards keep their status and move to `work/backburner/`.

### Progress notes

A card in progress should always say where it stands, so any session can pick it up cold. At the end of each working session on a card, append one dated line: the stage, what was done, and the next step. Keep them short; the history of how is in the commits.

### Dependencies

`Blocked by` lists hard dependencies. A card should not start until every blocker is `Done`. External blockers (such as the .NET 11 GA release) are written as plain text bullets without a link, for example `- External: .NET 11 / C# 15 GA`. The card is not ready while one is open; when it is met, strike it through (`- ~~External: …~~ Met YYYY-MM-DD.`) rather than deleting it.

`Blocked by` and `Unblocks` are mirror images: if A is blocked by B, B unblocks A. Always edit both sides. `scripts/work.py check` enforces this.

### Adding or changing cards

- New card IDs take the next unused number for the epic's prefix.
- Add the card to its epic table in `work/README.md`, and to its phase table in the right wave (the wave is one more than the highest wave among its blockers).
- Update `**Totals:**` in `work/README.md`.
- If the card belongs to no existing epic, create the epic: a new numbered folder, a new prefix, a section under `## Epics` in `work/README.md`.
- Scope changes to a card in progress are fine if they are small and noted in progress notes. Anything larger becomes a new card.

## Epics, phases, waves and gates

- **Epics** group cards by area and own a folder. An epic's prefix (`FND`, `CORE`, …) is its cards' ID prefix.
- **Phases** follow the whitepaper's [Roadmap](whitepaper.md#roadmap). A card can start as soon as its blockers are done, even if earlier phases still have unrelated open work.
- **Waves** group a phase's cards by dependency depth. Everything in one wave can run in parallel.
- **Gates** are cards with a `Gate` row. A gate card is done only when the gate's criterion is shown to hold, with evidence (test results, benchmark numbers) recorded in its Outcome. Numeric thresholds come from [FND-06](work/active/01-foundations/FND-06-fix-numeric-gate-thresholds.md).

## Research notes

A research note answers a question with evidence. File: `work/docs/research/YYYY-MM-DD-<slug>.md`, listed in the [research index](work/docs/research/README.md), which also holds the template.

Rules:

- **Every source is cited, with a link.** Only cite what was actually read: a paper, a repository, documentation, a benchmark. If a claim comes from memory and could not be checked, mark it *unverified*.
- **Separate what a source says from what we infer.** "Parlot reports X on its own benchmarks" is not "X is true".
- **Look for disconfirming evidence.** The most useful finding is often that another library already does what we planned, or that a cited result does not hold.
- **End with implications for Creole**: which whitepaper sections, cards or decisions it affects.

A research note is a record of what was learned at a point in time. It is not edited later to change its conclusions. If it goes stale, write a new note and move the old one to `work/docs/archive/` with a pointer to its replacement.

## Decision records

A decision record captures one design choice. File: `work/docs/decisions/NNNN-<slug>.md`, numbered in sequence, listed in the [decisions index](work/docs/decisions/README.md), which also holds the template.

- Status is `Proposed`, `Accepted`, `Rejected` or `Superseded by NNNN`.
- Only the maintainer accepts a decision. Claude may draft and recommend; it does not mark a record Accepted on its own.
- Once Accepted, a record is not rewritten. To change a decision, write a new record that supersedes it and update the old record's status.
- Write a record for any choice that someone could reasonably have made differently and that later work depends on: public API shape, semantics, data structures with performance consequences, gate thresholds, scope cuts. Do not write one for choices that are easy to reverse.

## Ideas

An idea is a thought not yet ready to be a card: a possible feature, a paper to read, a doubt about the design. File: `work/ideas/<slug>.md` with a few lines on what the idea is, why it might matter, and which whitepaper sections it touches. Ideas are listed in `work/ideas/README.md`.

An idea is promoted by creating the card or research note it leads to and deleting the idea file (the new artifact records where it came from), or dropped by deleting it with a one-line reason in the index.

## Documentation

`docs/` is written alongside the code, in the same change, never afterwards. [REL-02](work/active/14-release/REL-02-documentation.md) polishes and fills gaps for 1.0; it is not where documentation starts.

- Any change to public API or behaviour updates the relevant page in `docs/`.
- Every diagnostic ID (`CRLnnnn`) gets its reference page in the change that introduces it.
- Every public type and member has XML documentation comments.
- Docs describe what Creole does, for its users. Design rationale stays in the whitepaper and decision records; link to them rather than repeat them.

See [docs/README.md](docs/README.md) for the planned layout.

## Changing the whitepaper

The whitepaper is a living specification. It always describes the intended system, so it changes when the design changes:

- **Design changes** come through an Accepted decision record. The whitepaper edit happens in the same change as the acceptance, and the edited section links to the record.
- **Editorial fixes** (typos, inconsistencies, clarity) can be made directly, as part of a card such as [FND-08](work/active/01-foundations/FND-08-whitepaper-editorial-cleanup.md) or as a small standalone change.
- **Open questions** in § Risks and open questions are updated when a decision closes them: move the question to "Questions raised in review" with "Decided: …" and a link.
- Never change the whitepaper's design silently during Build. If building shows the whitepaper is wrong, stop, move the card to `Designing`, and record a decision.

Heading changes break anchors used by cards. After editing headings, run `scripts/work.py check`.

## Discrete changes and commits

Every change should be one understandable piece of work: a reviewer should be able to say what it does in one sentence.

- One card, or one stage of one card, per change. A research note, a decision record with its whitepaper edits, or a Build increment.
- Do not mix unrelated edits. Tracker bookkeeping for the card (status, progress notes, README lists) belongs in the same change as the work.
- If a change grows past what its card describes, split it: finish what the card covers and create a new card for the rest.

Commit messages:

```text
CORE-04: Add Seq and Map nodes

Optional body: what and why, not how. Mention decision records
and any deviation from the card.
```

Changes not tied to a card use an area prefix: `refactor:` (behaviour-preserving code changes), `work:` (tracker housekeeping), `whitepaper:` (editorial), `docs:`, `research:`, `tooling:`.

A mechanical refactor is one discrete change even when it touches many files, provided it is one transformation and changes no behaviour. Behaviour changes, fixes and cleanups found along the way are separate changes.

## Completing a card

A card is Done when:

1. Every acceptance criterion is met **and verified**: tests pass, measurements are recorded, or the item is inspected. Check each box only with evidence.
2. Code is tested, and documentation (`docs/`, XML docs) is updated with it.
3. Any deviation from the card or whitepaper has an Accepted decision record, and the whitepaper reflects it.
4. The card has an `## Outcome` section, and `Status` is `Done`.
5. `work/README.md` is updated: the card leaves "In progress", newly unblocked cards join "Can start now", and the card is marked ✅ in its tables.
6. `scripts/work.py check` passes.

If a criterion turns out to be wrong or unreachable, do not tick it. Change the card (noting why in progress notes) or record a decision, then complete it.

## Completing an epic

When every card in an epic is Done, move the epic folder to `work/complete/` with:

```sh
python3 scripts/work.py move work/active/<NN-epic> work/complete
```

The script rewrites every relative link into and out of the folder. Then update the epic's `Folder:` line in `work/README.md` and run `scripts/work.py check`. The same command moves epics or cards to and from `work/backburner/`.

## Keeping the tracker consistent

[`scripts/work.py`](scripts/work.py) does the mechanical checks so nobody has to:

| Command | What it does |
| --- | --- |
| `python3 scripts/work.py check` | Verifies every relative link and heading anchor in the repository, card status values, `Blocked by` ↔ `Unblocks` symmetry, Done cards having all criteria checked, README totals, and the README "In progress" and "Can start now" lists. |
| `python3 scripts/work.py status` | Counts by status and phase, in-progress cards, and cards ready to start. |
| `python3 scripts/work.py ready` | Cards whose blockers are all Done. |
| `python3 scripts/work.py card <ID>` | One card's status, blockers and dependents. |
| `python3 scripts/work.py move <path> <dest>` | Moves a file or folder and rewrites links. `--dry-run` previews. |

Run `check` before finishing any change that touches `work/`, `docs/` or the whitepaper.

## Working with Claude

Claude Code is set up to work inside this process. [CLAUDE.md](CLAUDE.md) holds the rules every session follows; the rest of the setup lives in `.claude/`:

| Piece | Location | What it does |
| --- | --- | --- |
| Skills | `.claude/skills/` | Multi-step workflows, run as slash commands or picked up by Claude when relevant. |
| Subagents | `.claude/agents/` | `researcher` answers one research question from primary sources; `critic` reviews or verifies work with a fresh context. |
| Rules | `.claude/rules/` | Short rules that load only when Claude touches matching files: cards, research notes, decision records, docs, the whitepaper. |
| Hooks | `.claude/settings.json` | At session start, a tracker brief (`work.py brief`). At the end of each turn, `work.py check`, with problems fed back to Claude. |
| Permissions | `.claude/settings.json` | Pre-approves the tracker script, read-only git, `dotnet build/test` and web research; blocks `git commit` and `git push`. |

| Skill | Use it to |
| --- | --- |
| `/status` | See where the project stands and what to do next. |
| `/start <ID> [research\|design\|build]` | Start a card at the right stage, or move it to the next. |
| `/research <question or ID>` | Investigate with outside sources and write a cited research note. |
| `/decide <topic or ID>` | Draft a decision record; `/decide accept NNNN` folds it into the whitepaper and cards. |
| `/build <ID>` | Plan against the acceptance criteria, then code, tests and docs together. |
| `/refactor <transformation>` | Make a behaviour-preserving change across the codebase in verified batches. |
| `/reconcile [ID] [paths or git range]` | Bring the card, README, docs and XML docs in line with work you did by hand. |
| `/complete <ID>` | Verify a card against evidence, check it off, and update everything downstream. |
| `/challenge <target>` | Get an independent red-team review. Read-only. |
| `/check-sources <file>` | Verify the citations and factual claims in a document. Read-only. |
| `/idea <text>` | Capture an idea before it is ready to be a card. |
| `/new-card <description>` | Add a card or epic with dependencies wired in. |
| `/sync` | Find and fix drift across cards, indexes, docs and the whitepaper. |

Habits that keep sessions effective:

- Start a fresh session (`/clear`) for each card or unrelated task. Cards carry their own progress notes, so nothing is lost.
- Plan before building anything non-trivial (`/build` does this; plan mode works too). Skip the plan when the diff fits in one sentence.
- If Claude goes wrong twice on the same thing, rewind (`Esc Esc`) or clear, and restate the task with what you learned, rather than correcting in place.
- When Claude keeps making the same mistake, add a line to the relevant rule file or a skill's "Gotchas" section. Keep CLAUDE.md short: it loads into every session.
- `/context` shows what is loaded; `/doctor prompt-audit` checks the instructions for conflicts and staleness.
