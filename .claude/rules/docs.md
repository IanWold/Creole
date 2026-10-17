---
paths:
  - "docs/**/*.md"
---

# Editing user documentation

Full rules: CONTRIBUTING.md § Documentation.

- `docs/` describes Creole for its users, and must be true of the current code. Do not document planned behaviour here; that is the whitepaper's job.
- Docs change in the same change as the code they describe.
- One page per diagnostic: `docs/reference/diagnostics/CRLnnnn.md`, with what triggers it, an example grammar that triggers it, and how to fix it.
- Link to the whitepaper or decision records for rationale instead of repeating it.
- Code samples must compile against the current API. Prefer samples taken from tests or samples/ so they stay checked.
- Add every new page to the index in docs/README.md.
