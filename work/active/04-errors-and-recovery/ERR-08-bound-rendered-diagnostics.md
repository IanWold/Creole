# ERR-08: Bound rendered diagnostics

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

Keep a rendered diagnostic small whatever the input, so rendering an error on untrusted input cannot produce megabytes of text.

## Scope

- Two measurements from the [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md), finding 24, both the untrusted-input case § Limits for untrusted input addresses:
  - one error on a 617,799-character minified line renders to 1,235,763 characters: the whole line, plus a caret line padded to the column;
  - a depth-limit error on 1,500 nested `[` prints 1,000 context notes, about 40 KB.
- Bound only rendered text. `ParseError` and the failure log are unchanged ([ADR-0029](../../docs/decisions/0029-rendered-errors-bound-text-from-input-with-caller-set-limits.md) point 1).
- `RenderOptions`, a record like `ParseOptions`, with `MaxWidth` (default 120, at least 10) and `MaxNotes` (default 10, at least 0); `int.MaxValue` lifts either. `Diagnostics` gains `Render(RenderOptions)` and `Render(RenderOptions, params ReadOnlySpan<SourceText>)`. The existing overloads and `ParseError.ToString()` use `RenderOptions.Default` (point 2). Check whether `Render(null)` is ambiguous.
- Measure in display columns (`DisplayWidth`). A cluster counts as at least one column and shows at most 32 scalar values (point 3).
- Show a window of a long source line, centred on the span, with `…` where text is left out. A span wider than the window shows its start, with carets to the edge. Tabs keep their stops from the full line (point 4).
- Cut an unexpected item's display to `MaxWidth` with a trailing `…`, and replace its control characters as a source line's are (point 5).
- Past `MaxNotes`, keep the innermost `⌈MaxNotes / 2⌉` and outermost `⌊MaxNotes / 2⌋` context lines, with one `… N more` line between them, in the block and in the one-line form (point 6).
- Update docs/concepts/errors.md: § Rendering against the source, § Caret alignment (display strings are no longer printed as given) and § One line per error.

## Acceptance criteria

- [ ] A test renders one error on a line of 600,000 characters, and each output line stays within a fixed length.
- [ ] A test renders a depth-limit error with 1,000 context frames as a bounded number of note lines, one of which says how many were left out.
- [ ] A test renders the same depth-limit error in the one-line form, through `ParseError.ToString()` and through `Render()` without source text, with at most `MaxNotes` context lines and one entry that says how many were left out.
- [ ] Tests render an error whose unexpected token has a 600,000-character `Detail`, an error on a line holding one cluster of 600,000 scalar values, and an error on a line of 600,000 zero-width characters, and each output line stays within a fixed length.
- [ ] Caret tests cover lines wider than `MaxWidth`, with tabs and wide characters at the window's edges and spans wider than the window.
- [ ] A test shows that `int.MaxValue` for both limits renders whole lines and every note, and that smaller limits narrow the output.

## Blocked by

- [ERR-03 Diagnostic rendering](ERR-03-diagnostic-rendering.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input)
- [§ The error model](../../../whitepaper.md#the-error-model)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
- [ADR-0029](../../docs/decisions/0029-rendered-errors-bound-text-from-input-with-caller-set-limits.md) Rendered errors bound the text that comes from input, with limits the caller sets in `RenderOptions`

## Progress notes

- 2026-10-27 (Designing): Started. The whitepaper does not bound rendered output, so a decision record settles the limits (fixed or caller-set), the window and note policy, and whether the one-line form and a token's `Detail` are bounded too, before Build.
- 2026-10-27 (Designing): Drafted ADR-0029 (Proposed) after prior art on rustc, clang and CPython and the maintainer's choices: caller-set limits in `RenderOptions`, innermost and outermost notes kept, and the one-line form, token `Detail` and long or zero-width clusters bounded too. Next: the maintainer accepts, rejects or revises ADR-0029; on acceptance, widen this card's scope and criteria and move to Build.
- 2026-10-27 (Building): ADR-0029 accepted; whitepaper updated, and scope and criteria widened to match it. Next: `/build` to plan the change against the six criteria.
