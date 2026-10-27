# ERR-08: Bound rendered diagnostics

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 1 — Layers |
| Status | Done |

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

- [x] A test renders one error on a line of 600,000 characters, and each output line stays within a fixed length.
- [x] A test renders a depth-limit error with 1,000 context frames as a bounded number of note lines, one of which says how many were left out.
- [x] A test renders the same depth-limit error in the one-line form, through `ParseError.ToString()` and through `Render()` without source text, with at most `MaxNotes` context lines and one entry that says how many were left out.
- [x] Tests render an error whose unexpected token has a 600,000-character `Detail`, an error on a line holding one cluster of 600,000 scalar values, and an error on a line of 600,000 zero-width characters, and each output line stays within a fixed length.
- [x] Caret tests cover lines wider than `MaxWidth`, with tabs and wide characters at the window's edges and spans wider than the window.
- [x] A test shows that `int.MaxValue` for both limits renders whole lines and every note, and that smaller limits narrow the output.

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
- 2026-10-27 (Building): Increment 1 done: `RenderOptions` with `MaxNotes` only (`MaxWidth` comes with the code that uses it), `Render(RenderOptions)` and `Render(RenderOptions, params ReadOnlySpan<SourceText>)`, and innermost and outermost notes with `… N more` in the block and the one-line form. Tests pin criteria 2 and 3 through real parses of 1,500 `[` (1,001 context lines, 991 left out). Found, as ADR-0029 asked: `Render(null)` and `Render(null, text)` no longer compile (CS0121, ambiguous with the `params ReadOnlySpan<SourceText>` overload). Both were runtime errors before, so no working call breaks. Next: increment 2, the source-line window, cluster rules and the cut unexpected display (criteria 1, 4, 5 and the rest of 6).
- 2026-10-27 (Building): Increment 2 done: `RenderOptions.MaxWidth` (default 120, at least 10); `DisplayWidth.ShowLine` windows a long line by cells (a cluster takes at least one), centred, shifted at the line's ends, starting at the span when it is wider, leaving out clusters that cross an edge, with tabs keeping their whole-line stops; a cluster shows at most 32 scalar values; `DisplayWidth.Cut` cuts an unexpected item's display and replaces its control characters, in the block and the one-line form. Every criterion appears met, by `DiagnosticRenderingTests.AnErrorOnAVeryLongLineShowsAWindowAroundIt` (1), `ParsingTests.ADeepDepthErrorRendersItsInnermostAndOutermostNotes` (2), `ParsingTests.ADeepTokenDepthErrorRendersItsInnermostAndOutermostNotesOnOneLine` (3), `WorkedExampleATests.ATokenWithALongDetailRendersWithinTheWidth`, `AClusterOfManyMarksShowsItsFirst32ScalarValues` and `ALineOfZeroWidthCharactersShowsAWindowAroundTheSpan` (4), the window examples and the property `AWindowShowsTheStartOfTheSpanUnderItsCarets` in `DisplayWidthTests` (5), and `LimitsOfIntMaxValueShowEverythingAndSmallerOnesShowLess` (6). Next: `/complete`.
- 2026-10-27 (Done): Verified each criterion on `ebaefab`: all 1,596 tests pass, and each criterion's tests run and pass by name.

## Outcome

Rendering an error on untrusted input is now bounded, as [ADR-0029](../../docs/decisions/0029-rendered-errors-bound-text-from-input-with-caller-set-limits.md) decided. The review's two cases now render small. The 617,799-character line becomes a 120-column window, and the 1,000 context notes become 10 plus a count.

- **`RenderOptions`** ([RenderOptions.cs](../../../src/Creole/Errors/RenderOptions.cs)): `MaxWidth` (default 120, at least 10) and `MaxNotes` (default 10, at least 0); `int.MaxValue` lifts either. `Diagnostics` gains `Render(RenderOptions)` and `Render(RenderOptions, params ReadOnlySpan<SourceText>)` ([Diagnostics.cs](../../../src/Creole/Errors/Diagnostics.cs)); the other overloads and `ParseError.ToString()` use the defaults.
- **Notes** ([DiagnosticRenderer.cs](../../../src/Creole/Errors/DiagnosticRenderer.cs), [ParseError.cs](../../../src/Creole/Errors/ParseError.cs)): past `MaxNotes`, the innermost and outermost halves with `… N more` between them, in the block and the one-line form.
- **Widths** ([DisplayWidth.cs](../../../src/Creole/Errors/DisplayWidth.cs)):
  - a long line is shown as a window measured in cells (a cluster takes at least one);
  - a cluster shows at most 32 scalar values;
  - an unexpected item's display is cut to `MaxWidth`, with its control characters replaced.
- **Tests:** `RenderOptionsTests`, `DisplayWidthTests` (window examples and a CsCheck property), and 600,000-character cases in `DiagnosticRenderingTests`. `ParsingTests` and `WorkedExampleATests` cover real parses: 1,500 nested `[` over characters and tokens, and a JSON string token with a 600,000-character detail.
- **Docs:** [docs/concepts/errors.md](../../../docs/concepts/errors.md) § Limits on what is shown, § Caret alignment and § One line per error.

Deviations: none from ADR-0029.

Found while building:

- `Render(null)` and `Render(null, text)` no longer compile (CS0121). Both threw at run time before.
- A span wider than the window starts the window at the span, as ADR-0029 point 4 decided. So `…"hhh…` hides short context that would have fit before a wide token. This is a candidate for a later record if it matters in use.
- `scripts/work.py` gives a heading made only of inline code an empty anchor. The new docs section has a plain title to avoid this. The checker fix is a separate `tooling:` change.
- The number of errors is not bounded; ERR-05 decides whether to cap it.
