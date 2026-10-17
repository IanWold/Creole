---
name: idea
description: Capture, list, promote or drop an idea for Creole in work/ideas/ — the stage before Research. Use when the maintainer floats a thought, a paper to read, a possible feature or a doubt that is not ready to be a card.
argument-hint: "<idea text> | list | promote <slug> | drop <slug> <reason>"
---

# Ideas

Rules: CONTRIBUTING.md § Ideas. Capturing should be fast; do not research or design here.

## Input

- Free text: capture a new idea.
- `list`: show ideas from `work/ideas/README.md`.
- `promote <slug>`: turn an idea into a card or research note.
- `drop <slug> <reason>`: delete it, keeping a one-line record.

## Capture

1. Create `work/ideas/` and `work/ideas/README.md` if missing. The README has a short intro linking CONTRIBUTING § Ideas, an `## Ideas` table (`Idea | Added | Touches | Summary`), and a `## Dropped` list.
2. Check existing ideas, cards and research for the same thought. If it exists, add to that instead and say so.
3. Write `work/ideas/<slug>.md`:

   ```markdown
   # <Idea in a few words>

   Added YYYY-MM-DD.

   **What:** two or three sentences.
   **Why it might matter:** the goal or problem it serves.
   **Touches:** whitepaper sections, epics or cards.
   **First question:** what would need to be true for this to be worth doing.
   ```

4. Add it to the index. Give a one-line honest first reaction (including "this conflicts with § X" if it does), then stop.

## Promote

Decide with the maintainer whether it becomes a research note (`/research`) or a card (`/new-card`). Create that artifact, mentioning the idea in it, then delete the idea file and its index row.

## Drop

Delete the file, remove its index row, and add `- <slug> (YYYY-MM-DD): <reason>` under `## Dropped`.

Run `python3 scripts/work.py check` after any change.
