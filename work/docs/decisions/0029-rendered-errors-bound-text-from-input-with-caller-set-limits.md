# ADR-0029: Rendered errors bound the text that comes from input, with limits the caller sets in `RenderOptions`

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-27 |
| Cards | [ERR-08](../../active/04-errors-and-recovery/ERR-08-bound-rendered-diagnostics.md), [STRM-03](../../active/08-streaming/STRM-03-streaming-diagnostics.md), [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md) |
| Whitepaper | [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input), [§ The error model](../../../whitepaper.md#the-error-model), [§ Results](../../../whitepaper.md#results-1), [§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input) |
| Research | [Phase 0 review](../research/2026-10-25-phase-0-review.md) finding 24; prior art read for this record by a subagent on 2026-10-27, cited inline |
| Supersedes | — |

## Context

[§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input) bounds recursion and buffering, so a hostile input ends in an ordinary diagnostic. Nothing bounds how large that diagnostic is once it is rendered. The [Phase 0 review](../research/2026-10-25-phase-0-review.md), finding 24, measured two cases:

- One error on a 617,799-character minified line renders to 1,235,763 characters: the whole line, then a caret line padded to the column.
- A depth-limit error on 1,500 nested `[` prints 1,000 context notes, about 40 KB.

Reading the renderer for this record ([DiagnosticRenderer.cs](../../../src/Creole/Errors/DiagnosticRenderer.cs), [DisplayWidth.cs](../../../src/Creole/Errors/DisplayWidth.cs), [ParseError.cs](../../../src/Creole/Errors/ParseError.cs), [Display.cs](../../../src/Creole/Errors/Display.cs)) found four more ways for input to make the output large:

- **The one-line form.** `ParseError.ToString()`, and `Render()` when it lacks a source text, print every context line. A token parse always renders this way, so a depth-limit error there gets past any bound on the block.
- **An unexpected item's display.** A token shows its kind's description and then its `Detail` ([ADR-0023](0023-token-types-describe-their-kinds.md)), which usually comes from input. A 600,000-character string token makes a 600,000-character headline or caret label. This text is also printed as given, without the control-character replacement that source lines get.
- **Long grapheme clusters.** A letter followed by 600,000 combining marks is one cluster, one column wide. A window measured in columns shows all of it.
- **Zero-width clusters.** A line of 600,000 U+200B characters is 600,000 clusters of zero columns. A window measured in columns shows all of them as well.

Text the grammar author writes is not input: layer names, labels, expected items, messages and context-line text. Its size is fixed by the grammar.

Constraints:

- **Errors are data.** [§ Equivalence](../../../whitepaper.md#equivalence) has both engines agree on every diagnostic's span and expected set. Editors and tools read `ParseError`, not rendered text.
- **Rendering is off the hot path.** It runs only for a failed parse that someone renders.
- **Streamed errors capture a snippet when raised.** [§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input) caps that snippet at "a configurable length" (STRM-03). A render-time window can only show what was captured.

Prior art, read on 2026-10-27 through a fetch tool that summarises pages, so line-level details are approximate:

| Tool | Long source lines | Long context lists |
| --- | --- | --- |
| rustc, through annotate-snippets since [rust-lang/rust#150032](https://github.com/rust-lang/rust/pull/150032) | Trimmed to `--diagnostic-width`, else the terminal's width, else `DEFAULT_TERM_WIDTH: usize = 140` ([mod.rs](https://github.com/rust-lang/annotate-snippets-rs/blob/main/src/renderer/mod.rs), [the rustc book](https://doc.rust-lang.org/rustc/command-line-arguments.html#--diagnostic-width-specify-the-terminal-width-for-diagnostics)). The window centres the span, and a cut edge shows `...`, or `…` in Unicode output ([margin.rs](https://github.com/rust-lang/annotate-snippets-rs/blob/main/src/renderer/margin.rs)). A span wider than twice the width has its middle cut out, in the code line and the underline ([graphics.rs](https://github.com/rust-lang/annotate-snippets-rs/blob/main/src/renderer/graphics.rs)). | — |
| Clang | Trimmed around the caret only when a message length is set, which defaults to 0 ([TextDiagnostic.cpp](https://github.com/llvm/llvm-project/blob/main/clang/lib/Frontend/TextDiagnostic.cpp)). | `-ftemplate-backtrace-limit` defaults to 10 and `-fmacro-backtrace-limit` to 6 ([DiagnosticOptions.h](https://github.com/llvm/llvm-project/blob/main/clang/include/clang/Basic/DiagnosticOptions.h)). Template backtraces keep `Limit / 2 + Limit % 2` frames from the start and `Limit / 2` from the end ([SemaTemplateInstantiate.cpp](https://github.com/llvm/llvm-project/blob/main/clang/lib/Sema/SemaTemplateInstantiate.cpp)) and print one note between them: "(skipping %0 context%s0 in backtrace; use -ftemplate-backtrace-limit=0 to see all)" ([DiagnosticSemaKinds.td](https://github.com/llvm/llvm-project/blob/main/clang/include/clang/Basic/DiagnosticSemaKinds.td)). 0 means no limit. |
| CPython | — | Identical consecutive frames collapse after 3 into "[Previous line repeated N more times]", and `sys.tracebacklimit` defaults to 1,000 ([traceback.py](https://github.com/python/cpython/blob/main/Lib/traceback.py), [sys docs](https://docs.python.org/3/library/sys.html#sys.tracebacklimit)). |
| Superpower | Renders no source line. Cuts the unexpected text to 12 characters plus `...` ([Friendly.cs](https://github.com/datalust/superpower/blob/dev/src/Superpower/Util/Friendly.cs)). | — |
| Pidgin, Parlot, ariadne, miette | No horizontal trimming found. miette wraps lines at a default `termwidth` of 200 ([graphical.rs](https://github.com/zkat/miette/blob/main/src/handlers/graphical.rs)). | No cap found. |

Neither rustc nor clang caps line length strictly: rustc's width is a target, and clang does not trim at all by default. No renderer found pairs a window with a capped context list as this record does.

## Options for where the bound applies (fork 1)

### 1A. When rendering (recommended)

`ParseError` keeps everything. `Render`, and `ToString` for the one-line form, cut what they show.

- **For.** Errors stay exact data, so engine agreement and tools are unaffected. One place enforces the bound. Rendering is cold, so the cost does not matter.
- **Against.** A `ParseError` still holds 1,000 context lines and a 600,000-character `Detail`. That memory is bounded by `MaxDepth` and by the input the caller already holds.

### 1B. When the error is built

The failure log keeps at most N context lines, and `Display.Token` cuts `Detail`.

- **For.** Smaller errors.
- **Against.** It changes what both engines must agree on, and loses information a tool may want, such as the outermost construct. Long source lines still need a window at render time, so the bound lives in two places.

### 1C. Do nothing

Document that callers who render untrusted input should cut the output.

- **Against.** Cutting rendered text after the fact breaks the caret line. Every caller would rebuild the renderer.

## Options for who sets the limits (fork 2)

### 2A. Fixed constants

- **For.** No new API. A later overload with limits is additive.
- **Against.** A CLI that knows its terminal width cannot use it, and a caller who wants the whole line cannot get it from `Render`.

### 2B. The caller, through `RenderOptions` (chosen by the maintainer)

An immutable record mirroring `ParseOptions`, with defaults.

- **For.** It matches rustc's `--diagnostic-width` and clang's backtrace limits, both settable with a value that turns the limit off. The shape is already familiar from `ParseOptions`.
- **Against.** New public API: a type, two `Render` overloads, docs and validation.

## Options for which context notes are kept (fork 3)

### 3A. The innermost few, then a count

- **For.** Simplest.
- **Against.** In runaway nesting the innermost notes are all alike, and the one that says where the nesting began is lost.

### 3B. The innermost and the outermost, with a count between (chosen by the maintainer)

Clang's rule for template backtraces.

- **For.** It keeps the most specific construct and the one that began the nesting.
- **Against.** Two runs to print instead of one.

### 3C. Collapse identical runs, as CPython does

- **Against.** Context notes carry positions, so 1,000 nested arrays give 1,000 different lines and nothing collapses.

## Options for a span wider than the window (fork 4)

### 4A. Show its start, with carets to the window's edge (recommended)

- **For.** Simple, and the location line already gives the start.
- **Against.** The end of the span is not shown.

### 4B. Cut out its middle, as rustc does

- **For.** Both ends are visible.
- **Against.** Two cuts in one line and in its carets, for a case that arises only on hostile or minified input.

## Decision

1. **Errors keep everything; rendering bounds it (1A).** `ParseError` and the failure log are unchanged. Every limit below applies to rendered text: the block from `Render`, and the one-line form from `ParseError.ToString()` and from `Render()` when it lacks a source text.
2. **`RenderOptions` (2B).** A `sealed record` like `ParseOptions`, immutable, varied with `with`:

   | Property | Default | Meaning |
   | --- | --- | --- |
   | `MaxWidth` | 120 | The most columns that text from input takes in one place: the source excerpt, and the unexpected item's display. At least 10, so two `…` marks, a tab and a wide character always fit. `int.MaxValue` shows whole lines and whole displays, as now. |
   | `MaxNotes` | 10 | The most context lines shown for one error. At least 0. `int.MaxValue` shows them all. |

   `RenderOptions.Default` holds the defaults. Setting a value below its minimum throws `ArgumentOutOfRangeException`.

   `Diagnostics` gains `Render(RenderOptions options)` and `Render(RenderOptions options, params ReadOnlySpan<SourceText> sources)`. The existing `Render()` and `Render(params ReadOnlySpan<SourceText>)` use `RenderOptions.Default`, and so does `ParseError.ToString()`.

   The default of 120 leaves room for a gutter of up to 13 columns (a ten-digit line number and ` | `) within rustc's 140-column default.
3. **Columns and clusters.** Text from input is measured as `DisplayWidth` measures source lines. Two rules close the cases a column count misses:
   - A cluster counts as at least one column against `MaxWidth`, even when it shows as zero, so zero-width clusters cannot pile up.
   - A cluster shows at most its first 32 scalar values, and the rest are dropped. Only stacked combining marks make a cluster that long. (That emoji sequences stay well under 32 is *unverified*.) The dropped marks have no width, so carets do not move.
4. **The source excerpt.** If the line fits in `MaxWidth`, it is shown whole. Otherwise:
   - The excerpt takes at most `MaxWidth` columns, counting `…` (U+2026, one column) at each edge where text was left out. A cluster or tab that would cross an edge is left out.
   - If the marked clusters fit, they are centred, with equal context on each side, shifted where the line ends first.
   - If they do not fit (4A), the excerpt starts at the span's first cluster, and the carets run to the column before the trailing `…`.
   - Tabs keep the stops they have in the full line, so their widths do not change with the window.
   - The location line still shows the true line and column.
5. **The unexpected item's display.** Wherever it is shown, it is measured as in point 3, cut to `MaxWidth` columns with a trailing `…`, and its control characters are replaced as a source line's are. This covers token `Detail` and any item's `ToString()`. Messages, labels, expected items and layer names are author text and stay as given.
6. **Context notes (3B).** If an error has more context lines than `MaxNotes`, the innermost `⌈MaxNotes / 2⌉` and the outermost `⌊MaxNotes / 2⌋` are shown, innermost first. One line between them says how many were left out:

   ```text
     = note: in array starting at 1:1000
     = note: in array starting at 1:999
     = note: in array starting at 1:998
     = note: in array starting at 1:997
     = note: in array starting at 1:996
     = note: … 990 more
     = note: in array starting at 1:5
     = note: in array starting at 1:4
     = note: in array starting at 1:3
     = note: in array starting at 1:2
     = note: in array starting at 1:1
   ```

   The one-line form shows the same lines, with `; … 990 more` in place of the note. The count line is shown even when `MaxNotes` is 0.
7. **The bound.** For a given grammar and options, each rendered line has a fixed maximum length, whatever the input. A source or caret line is at most the gutter plus `MaxWidth` columns, plus the unexpected display on the caret line. A line holds at most `MaxWidth × 32` scalar values, because of point 3. An error has at most `MaxNotes + 1` notes. Time stays linear in the length of the error's line.
8. **Streams.** STRM-03 captures a streamed error's snippet by point 4's rule, with a width it takes from the parse's options. `Render` then shows the captured excerpt, narrowed further if its `MaxWidth` is smaller.
9. **Not bounded here.** The number of errors. A parse that recovers (ERR-05) can report one error per recovery, so the whole output still grows with the input, though each error is bounded. A cap on the error count belongs with recovery.

Deciding reasons:

- **Fork 1.** Errors are data that engines must agree on and tools read. Rendering is the only place where size is a problem.
- **Fork 2.** The maintainer chose caller-set limits. rustc and clang both let the caller set them.
- **Fork 3.** The maintainer chose both ends. In runaway nesting, where it began is the useful fact, and clang keeps the same two runs.
- **Fork 4.** A span wider than 120 columns is a minified or hostile case. The start is enough, and the location line gives it.
- **Points 3 and 5.** Without them, a window measured in columns does not bound characters, and a token's `Detail` gets around the window.

What would change this record:

- If tools render through `ParseError.ToString()` and need it unbounded, it gains an overload that takes options.
- If users of the block ask to see where a long span ends, 4B replaces 4A.

## Consequences

- **Easier.** Rendering a parse of untrusted input is safe to log or print. A CLI can match its terminal's width.
- **Harder.**
  - New public API: `RenderOptions`, two `Render` overloads, and docs for them.
  - `ParseError.ToString()` no longer shows every context line or a token's full `Detail`. A caller who needs them reads `Context` and `Unexpected`.
  - ERR-03's docs say display strings are printed as given. An unexpected item's display now has its control characters replaced, as source lines do.
- **Ruled out.** Bounding errors when they are built (1B), unless a later record supersedes this one.
- **Risks.**
  - Overloads that take `RenderOptions` first, beside a `params ReadOnlySpan<SourceText>` overload, could make `Render(null)` ambiguous. The build checks this and records what it finds.
  - A window cut wrongly misplaces carets. ERR-03's caret tests run again with lines longer than `MaxWidth`.
- **Watched by** ERR-08's tests:
  - The review's two measurements, rendered with the defaults, each within a fixed length per line and `MaxNotes + 1` notes.
  - The four further cases from § Context: the one-line form, a token with a long `Detail`, a long cluster, and a line of zero-width clusters.
  - `int.MaxValue` for both limits reproduces today's output.

## Changes made

- Whitepaper:
  - [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input): a "Rendered errors" part with the `RenderOptions` table and the excerpt, column, note and bound rules. Links this record.
  - [§ The error model](../../../whitepaper.md#the-error-model): after the rendered example, one sentence that long lines and long runs of notes are cut.
  - [§ Results](../../../whitepaper.md#results-1): `Render` also takes a `RenderOptions`.
  - [§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input): a streamed error's snippet is the window rendering would show, at a width from the parse's options, and `Render` can narrow it.
- Cards:
  - [ERR-08](../../active/04-errors-and-recovery/ERR-08-bound-rendered-diagnostics.md): its open question is settled. Scope and criteria widen to `RenderOptions`, the one-line form, the unexpected item's display, and long and zero-width clusters.
  - [STRM-03](../../active/08-streaming/STRM-03-streaming-diagnostics.md): its snippet is captured by point 4's rule.
  - [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md): its scope notes that the number of errors is not bounded (point 9).
- Docs: docs/concepts/errors.md changes with ERR-08's build.
