---
paths:
  - "work/docs/decisions/**/*.md"
---

# Editing decision records

Full rules: CONTRIBUTING.md § Decision records. Template: work/docs/decisions/README.md.

- Only the maintainer accepts or rejects a decision. Draft with `Status: Proposed` and ask; never set `Accepted` on your own judgment.
- An Accepted record is immutable. The only permitted edits are its Status line (to `Superseded by NNNN`) and fixing broken links. To change a decision, write a new record that supersedes it.
- Numbers are sequential and never reused.
- Every record lists at least two real options, including the whitepaper's current position where there is one.
- On acceptance, the whitepaper is updated in the same change, and the record's "Changes made" section says what changed.
