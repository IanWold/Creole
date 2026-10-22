# ADR-0012: A custom scanner declares a character FIRST set or none, an optional name, and a yes-or-no result

| | |
| --- | --- |
| Status | Proposed |
| Date | 2026-10-22 |
| Cards | [CORE-12](../../active/02-core-combinators/CORE-12-custom-primitives.md), [INT-04](../../active/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md), [ANA-08](../../active/09-static-analysis/ANA-08-custom-scanner-contract-checks.md), [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md), [FUS-03](../../active/16-layer-fusion/FUS-03-fusion-with-modes-and-custom-scanners.md) |
| Whitepaper | [§ Custom primitives](../../../whitepaper.md#custom-primitives) |
| Research | — (raised when CORE-12 was started) |
| Supersedes | — |

## Context

[§ Custom primitives](../../../whitepaper.md#custom-primitives) gives this shape:

```csharp
sealed class RawString : IScanner<char, ReadOnlyMemory<char>>
{
    public ScannerInfo Info => new(Nullable: false, First: CharSet.Of('r'));
    public bool Scan(ref Cursor<char> c, out ReadOnlyMemory<char> value) { /* hand-written */ }
}
```

It leaves three gaps:

1. **FIRST for other input types.** `IScanner<TIn, TOut>` is generic over its input, but `First` is a `CharSet`. A scanner over tokens has no way to declare which kinds it starts with. The internal `FirstSet` can hold character codes, token kinds, union cases, literal tokens and ⊤ (any item), so the gap is in the public declaration, not in analysis.
2. **What a failure expects.** A failed `Custom` must record an expectation at its start position. The built-in character classes carry an optional name for this (`CharClassNode(set, name?)`, as in "expected digit"). The whitepaper does not say where a scanner's comes from.
3. **Why a scan failed.** `Scan` returns `bool` and must fail without consuming input. For the motivating case, an unterminated raw string, the scanner can only say "no" at the opening quote. It cannot report "unterminated raw string" at the end of the input.

Forces:

- **Simplicity:** the simple standard approach first, with room to extend later without breaking anything.
- **Analysis trusts the declaration** (§ Custom primitives), and ANA-08 checks it at run time in Debug builds (CRL0206). The declaration should be easy to check.
- **G7, equivalence:** the expectations a skipped attempt contributes under dispatch must match running it ([ADR-0011](0011-a-cut-is-dropped-with-every-failed-attempt.md) point 6, ERR-02). A scanner whose expectation is fixed when the node is built lets dispatch record it without running the scanner.
- **[ADR-0001](0001-grammar-node-encoding-and-engine-dispatch.md):** `Custom` is the only way user code enters a grammar, so a token layer without `Custom` has no escape hatch.

## Options for FIRST (fork 1)

### 1A. `First` is a `CharSet`, or null for any item (chosen)

`ScannerInfo(bool Nullable, CharSet? First = null)`. Null means ⊤, the same as `Satisfy`. `Custom` rejects a non-null `First` with a `GrammarException` unless `TIn` is `char`.

- **Pros:**
  - The smallest public surface. The whitepaper sample stays valid.
  - It follows the `Satisfy` precedent for user code analysis cannot see into.
  - Precise token-kind support can be added later as new members, without breaking anything.
- **Cons:**
  - A token-layer scanner can't be analysed: it is opaque (CRL0201), kind-based dispatch cannot skip it, and `Longest` overlap checks assume it overlaps everything.
  - The mismatch between a `CharSet` and a token scanner is caught at grammar construction, not by the compiler.

### 1B. Factory methods on a non-generic `ScannerInfo`

`ScannerInfo.Chars(nullable, set)`, `ScannerInfo.Kinds<TKind>(nullable, params TKind[])` and `ScannerInfo.AnyItem(nullable)`.

- **Pros:**
  - Precise on both character and token layers.
  - Maps onto the parts `FirstSet` already has, with no new public set type.
- **Cons:**
  - Still not tied to `TIn`, so `Custom` must check the declaration against `TIn` when the node is built.
  - Changes the whitepaper sample.
  - Three entry points to document, and union cases and literal tokens are still missing.

### 1C. A generic `ScannerInfo<TIn>` over a public item-set type

- **Pros:** typed, and covers all four parts of a FIRST set.
- **Cons:**
  - The largest public surface, and `FirstSet`'s parts (`RangeSet` keys, cases, values) become public contract.
  - Token kinds need `TKind` as well as `TIn`, so even this is not fully typed.
  - More than Phase 0 needs.

### 1D. `Custom` on character grammars only, for now

Keep `IScanner<TIn, TOut>` generic, but offer `Custom` only where `TIn` is `char`.

- **Pros:**
  - Simplest. Every motivating case (raw strings, heredocs, nested comments) is a lexer token.
  - A token-layer `Custom` can be added later without breaking anything.
- **Cons:**
  - Token layers have no escape hatch until then, contrary to ADR-0001's "user code enters only through `Custom`".
  - Needs a whitepaper edit.

## Options for the expectation (fork 2)

### 2A. Derive it from the FIRST set

A failed `Custom` records each item of its `First` as an `ExpectedItem`. Authors write `.Named(...)` for anything better.

- **Pros:** no new API; reuses `Named`.
- **Cons:**
  - The default message misleads. The raw-string scanner reports "expected 'r'".
  - With a null `First` there is nothing to say.

### 2B. An optional `Name` on `ScannerInfo`, falling back to 2A (chosen)

`ScannerInfo(bool Nullable, CharSet? First = null, string? Name = null)`. A failed `Custom` records `Label(Name)` when there is a name, and the items of `First` when there is not.

- **Pros:**
  - Good messages by default.
  - The same name serves tracing, grammar export and the debugger.
  - The same shape as `CharClassNode(set, name?)`.
- **Cons:**
  - It overlaps with `Named`. The two compose: an outer `Named` replaces the scanner's label, as it replaces any other expectation.
  - One more field in the whitepaper sample.

### 2C. A name parameter on `Custom`

`Custom(scanner, "raw string")`.

- **Pros:** visible where the scanner is used.
- **Cons:**
  - It is shorthand for `.Named(...)`.
  - One scanner used in two places can carry two names.

### 2D. The scanner's type name

- **Pros:** no extra code for the author.
- **Cons:** puts code identifiers ("RawString") into messages that end users read.

## Options for failure detail (fork 3)

### 3A. Keep `bool`, and document the pattern for detailed errors (chosen)

`Scan` stays as in the whitepaper. A scanner that wants to report a detailed error succeeds instead. It consumes the malformed text and returns a value that marks it. The grammar then fails with a message: with `Bind` ([CORE-10](../../active/02-core-combinators/CORE-10-bind-and-linq-query-syntax.md)) and `Fail(message)`, the failure follows consumed input, so it is committed and reported where the malformed text ends.

- **Pros:**
  - Matches the whitepaper.
  - The contract stays easy for ANA-08 to check: success or no consumption.
  - The expectation stays fixed at construction (fork 2), which dispatch needs.
- **Cons:**
  - The author must plan for it. A plain failure reports only the label at the start position.
  - The pattern needs `Bind`, an opaque node.

### 3B. A richer result

`Scan` returns a result carrying a failure position and a `CustomMessage`.

- **Pros:** precise errors with no pattern to learn.
- **Cons:**
  - A larger contract. Furthest-failure tracking must accept a position the scanner reports past its start, and "fails without consuming" needs redefining.
  - More for ANA-08 to check, and the expectation is no longer fixed, so dispatch cannot reproduce it without running the scanner.

### 3C. 3A now, and an optional second interface later

A future `IReportingScanner<TIn, TOut>` (name illustrative) could add 3B's result for scanners that opt in.

- **Pros:** additive; 3A does not block it.
- **Cons:** postpones the question.

## Decision

Options **1A**, **2B** and **3A, keeping 3C open**. Chosen by the maintainer in conversation on 2026-10-22.

1. **Declaration.**
   ```csharp
   public sealed record ScannerInfo(bool Nullable, CharSet? First = null, string? Name = null);
   ```
   `CharSet` becomes public, as CORE-12's scope already says.
2. **Construction.** `Custom(scanner)` reads `scanner.Info` once, when the node is built. Analysis and engines use that copy, so a scanner whose `Info` changes later has no effect on them. `Custom` throws `GrammarException` when:
   - the scanner is null (naming the caller's expression, as CORE-12's scope says);
   - `Info` is null;
   - `First` is non-null and `TIn` is not `char`;
   - `Name` is non-null and empty or white space.
3. **Analysis.** A `Custom` with a `First` has that FIRST set and is not opaque. A `Custom` without one has FIRST ⊤ and is opaque (CRL0201), like `Satisfy`. Its nullability is `Nullable` in both cases.
4. **Expectations.** A failed `Custom` records `Label(Name)` at its start position when it has a name. Otherwise it records each item of `First`, and records nothing when `First` is null. ERR-02 decides how `Satisfy` and ⊤ are rendered; `Custom` without `First` follows it. An outer `Named` replaces a `Custom`'s label as usual.
5. **Failure detail.** `Scan` returns `bool` and must fail without consuming input. The user docs show the pattern in 3A for errors such as an unterminated raw string. An opt-in reporting interface (3C) is left open; it would need its own record.

Deciding reason: each choice is the simplest that keeps every later extension additive. 1A keeps the whitepaper's sample and its generality at the cost of precision for token-layer scanners, which no current use case needs. 2B gives good messages by default with the shape the character classes already use. 3A keeps a scanner's expectation fixed at construction, which dispatch and the CRL0206 check both rely on.

What would change this:

- A real token-layer scanner whose opacity costs dispatch or fusion something that matters. 1B would then be added as new factory members.
- Users finding the 3A pattern too awkward for lexer errors. 3C would then be designed.

## Consequences

- **Easier:**
  - One record type to learn; the whitepaper sample is unchanged apart from the optional name.
  - A `Custom`'s FIRST set and expectation are known when the grammar is built, so analysis, dispatch and FUS-03 treat it like any other primitive.
  - ANA-08's check is plain: nullability, and the first item against `First` when there is one.
- **Harder:**
  - Token-layer scanners are opaque. A `Longest` over tokens with a `Custom` alternative cannot be checked for overlap.
  - A character scanner with no `First` runs on every character inside a fused `Longest`, which FUS-03 must allow for.
  - Detailed lexer errors need the 3A pattern and therefore `Bind`.
- **Ruled out for now:**
  - Precise FIRST sets for token-layer scanners.
  - Scanner-reported failure positions and messages.
- **Risks and how they are watched:**
  - Misuse of `First` on a token scanner is a construction-time `GrammarException`, covered by a CORE-12 test.
  - A lying declaration is CRL0206 at Debug run time (ANA-08).
- **Not part of this decision:** the sample returns `ReadOnlyMemory<char>`, but `Cursor<T>` exposes spans, so a token that crosses a segment boundary must be copied. The CORE-12 docs say so.

## Changes made

Filled in on acceptance.
