---
paths:
  - "work/docs/decisions/**/*.md"
---

# Editing decision records

Full rules: CONTRIBUTING.md § Decision records. Template: work/docs/decisions/README.md.

- Only the maintainer approves a record's options and criteria, accepts it, or rejects it. Draft with `Status: Proposed` and ask; never set `Accepted` on your own judgment.
- The first card in the `Cards` row owns the record. While that card builds, the Proposed record is its working specification and is revised in place: add an option, rule one out, change the recommendation.
- Fix the Criteria before any option is tried. A new option to try, or a change to the criteria, needs the maintainer's agreement first; date and explain a criteria change in the record.
- Never delete an option. Each settled option ends with a `**Result:**` line: chosen, or ruled out by an argument or a trial, with the trial's measurements and the commit it ran on.
- A record is ready to accept when one option meets the criteria and every other option is ruled out. For a record whose card builds code, acceptance comes in the card's final change.
- An Accepted record that has merged to `main` (`git log origin/main -- <file>`) is immutable. The only permitted edits are its Status line (to `Superseded by NNNN`) and fixing broken links. To change it, write a new record that supersedes it. Before it merges, correct it in place.
- Numbers are sequential and never reused.
- Every record lists at least two real options, including the whitepaper's current position where there is one.
- On acceptance, the whitepaper is updated in the same change, and the record's "Changes made" section says what changed.
