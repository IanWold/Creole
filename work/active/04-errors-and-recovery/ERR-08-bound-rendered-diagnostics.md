# ERR-08: Bound rendered diagnostics

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Keep a rendered diagnostic small whatever the input, so rendering an error on untrusted input cannot produce megabytes of text.

## Scope

- Two measurements from the [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md), finding 24, both the untrusted-input case § Limits for untrusted input addresses:
  - one error on a 617,799-character minified line renders to 1,235,763 characters: the whole line, plus a caret line padded to the column;
  - a depth-limit error on 1,500 nested `[` prints 1,000 context notes, about 40 KB.
- Show a window of a long source line around the span, marked with an ellipsis, and measure it in display columns (`DisplayWidth`).
- Collapse context notes beyond a small number into one line that says how many were left out.
- Decide whether the limits are fixed or set by the caller of `Render`.
- Update docs/concepts/errors.md § Rendering against the source.

## Acceptance criteria

- [ ] A test renders one error on a line of 600,000 characters, and each output line stays within a fixed length.
- [ ] A test renders a depth-limit error with 1,000 context frames as a bounded number of note lines, one of which says how many were left out.

## Blocked by

- [ERR-03 Diagnostic rendering](ERR-03-diagnostic-rendering.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input)
- [§ The error model](../../../whitepaper.md#the-error-model)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
