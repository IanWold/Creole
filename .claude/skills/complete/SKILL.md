---
name: complete
description: Complete a Creole card — verify every acceptance criterion against evidence, check them off, write the Outcome, mark it Done, update the work README and unblocked cards, check gates, and move the epic to work/complete/ when its last card closes.
argument-hint: "<card ID>"
---

# Complete a card

Rules: CONTRIBUTING.md § Completing a card and § Completing an epic.

## Input

`$ARGUMENTS`: a card ID.

## Workflow

1. **Verify each acceptance criterion** with evidence: run the tests, run the measurement, or inspect the artifact. For each, report `met (evidence)`, `not met (why)`, or `cannot verify (what is missing)`. Do not tick anything on intent or memory. For gate cards and L or XL cards, also ask the `critic` subagent to verify the criteria independently. Give it the card path and where the evidence is, but not your own verdicts. Reconcile any disagreement before going on.
2. **Check the definition of done**: tests exist and pass; `docs/` and XML docs are updated; every deviation from the card or whitepaper has an Accepted decision record and the whitepaper reflects it; no unresolved conflict between the card, whitepaper and code.
3. **If anything is missing, stop.** List what is missing and leave the card as it is. If a criterion is wrong or unreachable, propose changing the card or a decision. Never tick it to close the card.
4. **Gate cards** (a `Gate` row): confirm the gate's criterion from the whitepaper Roadmap and FND-06 thresholds, with numbers. Put the evidence in the Outcome.
5. **Update the card**: tick the criteria, set `Status: Done`, and add `## Outcome`: what was delivered and where (paths, docs pages), deviations with decision links, follow-up cards, and gate evidence if any.
6. **Update work/README.md**: remove the card from "In progress" (restore "None yet." if empty); mark it done in every table it appears in by putting ✅ before its link; add newly ready cards to "Can start now" (`python3 scripts/work.py ready`).
7. **Tell the maintainer what unblocked**: `python3 scripts/work.py card <ID>` lists dependents and what each still waits on.
8. **Epic complete?** If every card in the epic folder is Done, propose the move and, on agreement, run `python3 scripts/work.py move work/active/<NN-epic> work/complete`, then update the epic's `Folder:` line in work/README.md.
9. **Phase complete?** If this closes a phase's gate, say so and note any whitepaper Roadmap text to update.
10. Run `python3 scripts/work.py check`, fix anything reported, and give the commit message: `<ID>: Complete <title>` (or fold it into the final Build commit if nothing was committed since).
