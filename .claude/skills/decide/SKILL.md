---
name: decide
description: Drive a Creole design decision — draft a decision record with options and a recommendation, and once the maintainer accepts it, update the whitepaper, open questions and cards in the same change. Use for the Design stage of a card, or any choice later work depends on.
argument-hint: "<topic or card ID> | accept <NNNN> | reject <NNNN>"
---

# Decide

Rules: CONTRIBUTING.md § Decision records and § Changing the whitepaper. Only the maintainer accepts or rejects a decision.

## Input

- `<topic or card ID>`: draft a new decision record.
- `accept <NNNN>`: the maintainer has accepted record NNNN; fold it into the project.
- `reject <NNNN>`: mark it Rejected and record the reason.

## Draft workflow

1. **Gather context.** Read the card (if any), the whitepaper sections involved, related research notes, and earlier decisions (one may already cover this, or be superseded by this).
2. **Check readiness.** If the choice turns on facts nobody has established, say so and propose `/research` first instead of guessing.
3. **Lay out real options**, at least two, including the whitepaper's current position and "do nothing / defer" where that is plausible. For each: what it is, pros, cons, evidence, and effect on the whitepaper's goals and non-goals (§ Design goals and non-goals), performance, AOT, and the analyzers.
4. **Recommend one**, with the deciding reason, and say what would change your mind. If the choice turns on the maintainer's priorities rather than facts (API taste, what to give up), interview them with AskUserQuestion, one question per fork, before writing the record.
5. **Write** `work/docs/decisions/NNNN-<slug>.md` (next number) from the template in [work/docs/decisions/README.md](../../../work/docs/decisions/README.md), with `Status: Proposed`. Add it to the index. Link it from the card's `## Artifacts` and append a progress note. If the card is not already `Designing`, set it.
6. **Ask the maintainer** to accept, reject or revise. Do not mark it Accepted yourself, and do not touch the whitepaper yet.

## Accept workflow

1. Set the record's `Status: Accepted`, update the index.
2. **Edit the whitepaper** to describe the decided design, in every section affected (search for related terms, not just the obvious section). Link the record from the main section affected. If the decision closes an item in § Risks and open questions → Still open, move it to "Questions raised in review" with "Decided: …".
3. If the record supersedes an earlier one, set that one's status to `Superseded by NNNN`.
4. **Update cards**: scope or criteria changed by the decision, new cards it creates (use `/new-card` rules), and blockers it resolves. Fill in the record's "Changes made".
5. Move the card on: to `Building` if code follows, or complete it with `/complete` if the decision was the card's deliverable.
6. Run `python3 scripts/work.py check` (heading changes in the whitepaper can break card anchors). Suggest a commit message: `<ID>: Decide <choice> (ADR-NNNN)`.

## Reject workflow

Set `Status: Rejected`, add a "Rejection" paragraph with the maintainer's reason, update the index, and append a progress note to the card saying what happens next.
