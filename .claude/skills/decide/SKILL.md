---
name: decide
description: Drive a Creole design decision — draft a decision record with options, criteria and a recommendation; revise it while its card's build tries the options; and once the maintainer accepts it, update the whitepaper, open questions and cards in the same change. Use for the Design stage of a card, when a build changes a Proposed record, or for any choice later work depends on.
argument-hint: "<topic or card ID> | revise <NNNN> | accept <NNNN> | reject <NNNN>"
---

# Decide

Rules: CONTRIBUTING.md § Decision records and § Changing the whitepaper. Only the maintainer approves a record's options and criteria, accepts it, or rejects it.

A record that code depends on stays Proposed through its card's build. The build tries the options that need trying and revises the record, and the maintainer accepts it when the card completes. A record with no code behind it is accepted at the end of Design.

## Input

- `<topic or card ID>`: draft a new decision record.
- `revise <NNNN>`: change a Proposed record during its card's build.
- `accept <NNNN>`: the maintainer has accepted record NNNN; fold it into the project.
- `reject <NNNN>`: mark it Rejected and record the reason.

## Draft workflow

1. **Gather context.** Read the card (if any), the whitepaper sections involved, related research notes, and earlier decisions (one may already cover this, or be superseded by this).
2. **Check readiness.** If the choice turns on facts nobody has established and a trial cannot establish, say so and propose `/research` first instead of guessing.
3. **Lay out real options**, at least two, including the whitepaper's current position and "do nothing / defer" where that is plausible. For each: what it is, pros, cons, evidence, and effect on the whitepaper's goals and non-goals (§ Design goals and non-goals), performance, AOT, and the analyzers.
4. **Fix the criteria** before any option is tried: how the options will be judged, concrete enough that a trial's result can be read against them. Use gate thresholds ([ADR-0003](../../../work/docs/decisions/0003-numeric-gate-thresholds.md)), measurements with their inputs and number of runs, API properties, and tests that must pass.
5. **Plan the trials.** For each option, say whether an argument settles it or it needs a trial. Try only what argument cannot settle: speed, allocation, compiler behaviour, how the code reads. Treat independent decision points separately, and try combinations only where the points interact.
6. **Recommend one**, or name the trial that will decide, and say what would change your mind. If the choice turns on the maintainer's priorities rather than facts (API taste, what to give up), interview them with AskUserQuestion, one question per fork, before writing the record.
7. **Write** `work/docs/decisions/NNNN-<slug>.md` (next number) from the template in [work/docs/decisions/README.md](../../../work/docs/decisions/README.md), with `Status: Proposed`. Put the owning card first in its `Cards` row. Add it to the index. Link it from the card's `## Artifacts` and append a progress note. If the card is not already `Designing`, set it.
8. **Ask the maintainer** to approve the options, criteria and trial plan: the trial, not the answer. Do not touch the whitepaper.
   - If code follows, approval moves the card to `Building` with the record still Proposed. Update work/README.md's "In progress" line and offer `/build`.
   - If no code follows (the decision is the card's deliverable, or the record states a position on prior art), ask the maintainer to accept, reject or revise it instead.

## Revise workflow

Use during the owning card's build, when a trial or the build itself changes what the record says.

1. **Edit the record in place.** Never delete an option. Give each settled option its `**Result:**` line: chosen, or ruled out by an argument or a trial, with the trial's measurements and the commit it ran on. Add new options after the existing ones. Update the Decision section's recommendation.
2. **Ask before widening the trial.** A new option that needs a trial, or any change to the criteria, needs the maintainer's agreement before trials run. Date and explain a criteria change in the Criteria section.
3. **Append a progress note** to the card saying what changed and why.
4. If the record is Accepted, check whether it has merged (`git log origin/main -- <file>`). If it has, do not revise it: draft a superseding record. If it has not, correct it in place and say so in its "Changes made".

## Accept workflow

Usually runs from the owning card's `/complete`, or at the end of Design for a record with no code behind it.

1. **Check it is settled.** One option is chosen, meets the criteria, and the card's acceptance criteria are met with it. Every other option has a Result. The Decision section states the choice so someone could implement it, with the deciding reason. If anything is open, stop and say what.
2. Set the record's `Status: Accepted`, update the index.
3. **Edit the whitepaper** to describe the decided design, in every section affected (search for related terms, not just the obvious section). Link the record from the main section affected. If the decision closes an item in § Risks and open questions → Still open, move it to "Questions raised in review" with "Decided: …".
4. If the record supersedes an earlier one, set that one's status to `Superseded by NNNN`.
5. **Update cards**: scope or criteria changed by the decision, new cards it creates (use `/new-card` rules), and blockers it resolves. Fill in the record's "Changes made".
6. **Move the card on.** If this ran from `/complete`, return to it. If the decision was the card's deliverable, complete it with `/complete`. If the maintainer accepted early, before the build, the card goes to `Building`; from now on the decision changes only by a superseding record once it has merged.
7. Run `python3 scripts/work.py check` (heading changes in the whitepaper can break card anchors). Suggest a commit message: `<ID>: Decide <choice> (ADR-NNNN)`, or fold the acceptance into the card's completing change.

## Reject workflow

Set `Status: Rejected`, add a "Rejection" paragraph with the maintainer's reason, update the index, and append a progress note to the card saying what happens next.
