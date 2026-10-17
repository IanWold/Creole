---
name: reconcile
description: Bring Creole's cards, work README, docs and XML docs in line with work the maintainer did by hand — map the changes to a card's acceptance criteria, update status, progress notes and artifacts, draft the matching documentation, and flag deviations. Use when asked to "catch up", "update the tracker for what I did" or "sync the docs with my changes".
argument-hint: "[card ID] [paths or git range]"
---

# Reconcile

The maintainer did the work; your job is to make the project's records match it. You are reporting on someone else's change, so describe it accurately and judge criteria strictly. Do not change the code itself. If the code looks wrong, say so; do not fix it.

## Input

`$ARGUMENTS`: optionally a card ID, and optionally what to look at: paths, or a git range such as `HEAD~3..HEAD`. With no range, use uncommitted changes (`git status` and `git diff`, including untracked files). If the directory is not a git repository, ask the maintainer which files changed.

## Workflow

1. **See what changed.** Read the diff or the named files in full, not just their names. Summarize the change in two or three sentences.
2. **Find the card.** If none was given, match the change to cards by scope (`grep` over `work/active/`, and the in-progress list from `python3 scripts/work.py status`). If it fits several cards, or none, ask. Work that fits no card is either a refactor (`refactor:` commit, no card) or needs `/new-card`.
3. **Map the change to the acceptance criteria.** For each criterion, report `met (evidence)`, `partly met (what is missing)` or `not touched`. Evidence means tests that exist and pass, which you run, or an artifact you inspected. Never tick a criterion because the code looks like it should satisfy it.
4. **Check for deviations.** Compare the change with the card's scope, the linked whitepaper sections, and accepted decisions. List each difference. A small, local call goes in the progress note. Anything that changes public API, semantics or a later card's assumptions needs `/decide`; flag it and do not record it as settled.
5. **Present the plan before editing.** Show the criteria table, the deviations, and the record updates and doc drafts you intend to make, and let the maintainer correct you. Their judgment wins on what they meant the change to do; the evidence wins on whether a criterion is met.
6. **Update the records:**
   - Card: `Status` (`Building` if work remains; leave it unchanged if the card was already in that stage), ticked criteria with evidence only, a dated progress note describing what was done and what remains, and links in `## Artifacts` to new docs pages, decisions or notable code paths.
   - work/README.md: the "In progress" and "Can start now" lists.
7. **Update the documentation** to match the code as written, not as planned: `docs/` pages, a `docs/reference/diagnostics/CRLnnnn.md` page for each new diagnostic, and XML doc comments on new or changed public members. Use the code's actual names and behaviour. If you cannot tell what the intended behaviour is, ask rather than describe a guess.
8. **Check and hand off.** Run `python3 scripts/work.py check` and fix anything reported. If every criterion is met, offer `/complete`. Otherwise say what is left. Give a commit message covering the code and the records together (`<ID>: <what the maintainer's change does>`), or one for the records alone if the code is already committed (`<ID>: Update tracker and docs`).

## Gotchas

- Uncommitted changes can include unrelated work. If part of the diff does not belong to the card, leave it out and say so.
- A passing test suite is evidence only for what the tests actually check. Read the tests before citing them.
