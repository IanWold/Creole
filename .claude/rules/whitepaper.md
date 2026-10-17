---
paths:
  - "whitepaper.md"
---

# Editing the whitepaper

Full rules: CONTRIBUTING.md § Changing the whitepaper.

- The whitepaper is a living specification of the intended design. Design changes come only through an Accepted decision record, edited in the same change and linked from the section it changes.
- Editorial fixes (typos, inconsistencies, clarity, broken references) are fine without a decision. If an "editorial" fix changes what the system would do, it is a design change.
- Never change the design to match code that drifted from it. Report the drift instead.
- When a decision closes an item under § Risks and open questions → Still open, move it to "Questions raised in review" with "Decided: …" and a link.
- Renaming or rewording a heading breaks anchors that cards link to. Run `python3 scripts/work.py check` after any heading change and fix the links.
- Style: plain English, short declarative sentences, concrete claims, tables for comparisons. Cite claims about other libraries and papers; the References list is numbered.
