---
name: start
description: Start work on a Creole card — verify blockers, choose the entry stage (research, design or build), set its status, update the work README, and set up the stage's artifact. Also use to move a card to its next stage.
argument-hint: "<card ID> [research|design|build]"
---

# Start a card

## Input

`$ARGUMENTS`: a card ID (for example `CORE-04`) and optionally the stage to enter. If the card is already in progress, this moves it to the named stage.

## Workflow

1. **Load context.** Run `python3 scripts/work.py card <ID>`. Read the card, then the whitepaper sections it links to (only those), then any research or decisions in its `## Artifacts`.
2. **Check blockers.** If any blocker is not Done, or an external blocker (such as C# 15 GA) is unmet, stop and tell the maintainer which ones. Continue only if they explicitly choose to proceed anyway, and record that choice in a progress note.
3. **Choose the stage** if none was given, using CONTRIBUTING.md § The lifecycle ("Choosing the entry stage"). State the choice and the reason in one line. If it is unclear, ask.
4. **Check the whitepaper is settled for this card.** Look at § Risks and open questions and the card's sections for anything still open that this card depends on. If found, recommend Research or Design even if Build was requested.
5. **Update the card.** Set `Status` (`Researching`, `Designing` or `Building`). Add `## Progress notes` if missing and append `- YYYY-MM-DD (<Stage>): Started. <one line on approach>`.
6. **Update work/README.md.** Add the card to "In progress" (`- [ID](path) Title — Stage`), replacing "None yet." Remove it from "Can start now".
7. **Set up the stage:**
   - Research: draft the question(s) the research must answer, and offer to run `/research`.
   - Design: list the decisions the card needs, and offer to run `/decide`.
   - Build: offer to run `/build`, which plans before coding.
8. Run `python3 scripts/work.py check`, fix anything it reports, and suggest a commit message only if the maintainer wants to commit the status change on its own (usually it goes with the stage's first real work).
