# ERR-01: Error and result types

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

The public shapes for results and errors, using C# 15 unions as decided in FND-07.

## Scope

- `ParseError` with span, layer, unexpected, expected and context.
- `Unexpected` and `Expectation` unions with their case records.
- `ParseResult<T>` as the declared union `ParseResult<T>(Parsed<T>, Recovered<T>, Failed)` ([ADR-0002](../../docs/decisions/0002-parseresult-as-a-plain-union.md)). No public API returns or exposes `default(ParseResult<T>)`.
- `Diagnostics`, an immutable collection of `ParseError` ordered by span, with `Count`, `Empty` and `Render()`.
- `Render()` is minimal: one line per error. The rich format (source names, lines and columns, gutter, carets) is [ERR-03](ERR-03-diagnostic-rendering.md).

## Acceptance criteria

- [x] The whitepaper's `switch` on `json.Parse(text)` compiles without a default arm in an assembly that references Creole, and dropping a case is CS8509.
- [x] No public API returns `default(ParseResult<T>)`: a test lists every public member that returns `ParseResult<T>` and fails if one has no test showing its result is never `default`.
- [x] `Diagnostics` orders errors by span whatever order they arrive in, and keeps arrival order for equal spans (property test). `Count` and `Empty` are tested.
- [x] `Render()` writes one line per error in span order, with layer, span, unexpected, expected and context (snapshot test).

## Blocked by

- [FND-07 Validate C# 15 features and decide `ParseResult<T>`](../01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md)
- [SPAN-01 SourceSpan, SourceId and LineMap](../05-source-spans/SPAN-01-sourcespan-sourceid-and-linemap.md)

## Unblocks

- [INT-07 Recursion depth limit](../../complete/03-interpreter/INT-07-recursion-depth-limit.md)
- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)
- [ERR-03 Diagnostic rendering](../04-errors-and-recovery/ERR-03-diagnostic-rendering.md)
- [ERR-04 Diagnostic withdrawal](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md)
- [LAY-05 Pipeline API and sources](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)

## Whitepaper references

- [§ The error model](../../../whitepaper.md#the-error-model)
- [§ Results](../../../whitepaper.md#results-1)
- [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions)

## Artifacts

- [ADR-0002: `ParseResult<T>` is a plain C# 15 union](../../docs/decisions/0002-parseresult-as-a-plain-union.md)

## Progress notes

- 2026-10-18 (Build): Started. Design is settled by ADR-0002 and § The error model; build the records and unions, `ParseResult<T>`, and the span-ordered diagnostics collection, with a compile test of the whitepaper's switch. Next: `/build` plan.
- 2026-10-18 (Build): Increment 1 done: `ParseError` (element-wise equality over `Expected` and `Context`), `Unexpected`, `Expectation` and their case records in `src/Creole`, `ErrorModelTests` (13 pass; full suite passes), and `docs/concepts/errors.md`. Checked in scratch that Roslyn 5.9.0 reads Creole's unions from metadata: a full switch is clean and a missing case is CS8509, so the snippet test needs no package bump. Next: increment 2, results and `Diagnostics`.
- 2026-10-18 (Build): Increment 1 restructured before commit, at the maintainer's request: one type per file under `src/Creole/Errors/`, namespace `Creole`. Convention added to CONTRIBUTING.md § Conventions and `.claude/rules/dotnet.md`. Build and all tests pass. Next: increment 2, results and `Diagnostics`, also in `Errors/`.
- 2026-10-18 (Build): Increment 2 done: `Parsed<T>`, `Recovered<T>`, `Failed` (both reject empty diagnostics), `union ParseResult<T>` with `Diagnostics`, the span-ordered `Diagnostics` collection with `Render()`, and `ParseError.ToString()`, all in `src/Creole/Errors/`. Tests: `ParseResultTests` and `DiagnosticsTests`; full suite passes (227 in Creole.Tests). The snippet compiler moved to `tests/Creole.Tests/SnippetCompiler.cs` and now parses at `LanguageVersion.Preview` (union patterns are CS8652 otherwise). Found: the compiler emits a public constructor per case and no `op_Implicit`, so `new ParseResult<T>((Failed)null!)` can build a null-valued result; nullable warnings are the only guard. Every criterion appears met. Next: maintainer review, then `/complete ERR-01`.
- 2026-10-19 (Build): The maintainer changed `ParseResult<T>.Diagnostics` to switch over the three cases with no fallback, so a `default` result throws `SwitchExpressionException` there as in any switch. Test, XML doc and docs/concepts/errors.md updated to match; `DefaultHasNoDiagnostics` pins it. Full suite passes (228 in Creole.Tests). Next: `/complete ERR-01`.
- 2026-10-19 (Build): `/complete` held. All four criteria pass their tests (31/31), but the independent critic review found, and inspection confirmed: `ParseError` keeps caller lists by reference (so `Diagnostics` is not immutable) and accepts null lists and a default `Unexpected`; `Failed` has no `Deconstruct`; the no-default reflection scan skips the union's own members, keys by name only (overloads collide), and ignores parameters and protected members; the ordering property test breaks ties on `Layer`, which also carries the arrival index, so it cannot detect a sort by layer; the whitepaper sample still has the `_ => Diagnostics.Empty` arm. Next: a fix increment for these, then `/complete` again.
- 2026-10-19 (Build): Fix increment done. `ParseError` copies `Expected` and `Context` into read-only collections and rejects null parts and caseless union values; its `ToString()` switches on the unions with no fallback arm. `Failed` deconstructs. The no-default scan covers the union's own members, keys by declaring type and full signature, checks out and ref parameters and protected members, and its sanity test pins each kind. The ordering property test carries arrival order in `Context` with random layers; a mutation that sorted ties by layer made it fail. Whitepaper sample in § Using C# 15 unions updated editorially to the three-arm `Diagnostics` switch. Full suite passes (232 in Creole.Tests). Next: `/complete ERR-01`.
- 2026-10-19 (Build): `/complete` re-run at 4ef9b2a. All four criteria met on evidence (35/35 ERR-01 tests; critic re-verified, with mutations confirming the member scan and the ordering property test catch their regressions). Held on one finding: `ParseError`'s `Layer`, `Unexpected`, `Expected` and `Context` are get-only, so `with` fails with CS0200 for them, unlike the positional record in § The error model. Next: maintainer chooses `init` accessors that validate and copy, or a recorded deviation; then `/complete`.
- 2026-10-19 (Build): By the maintainer's choice, `ParseError`'s `Layer`, `Unexpected`, `Expected` and `Context` gained `init` accessors that check and copy as the constructor does, so `with` works again as for the positional record in § The error model. Tests `WithChangesEachPart` and `WithChecksAndCopiesLikeTheConstructor`; docs updated. Full suite passes (234 in Creole.Tests). Next: `/complete ERR-01`.

## Outcome

Delivered:

- The error model in [src/Creole/Errors/](../../../src/Creole/Errors/): `ParseError`, the `Unexpected` and `Expectation` unions and their case records. `ParseError` copies its lists, rejects nulls and caseless union values, and applies the same checks to `with`. Two errors are equal when their parts are equal element by element.
- `ParseResult<T>`, the declared union of `Parsed<T>`, `Recovered<T>` and `Failed` ([ADR-0002](../../docs/decisions/0002-parseresult-as-a-plain-union.md)). `Recovered<T>` and `Failed` require at least one error and deconstruct. `ParseResult<T>.Diagnostics` switches over the three cases, so a `default` result throws there as in any switch.
- `Diagnostics`: an immutable `IReadOnlyList<ParseError>` ordered by source, start and end, stable for equal spans, with `Empty`, `Count` and a minimal one-line-per-error `Render()` (also `ParseError.ToString()`). The rich format is [ERR-03](ERR-03-diagnostic-rendering.md).
- Tests: [ErrorModelTests.cs](../../../tests/Creole.Tests/ErrorModelTests.cs), [ParseResultTests.cs](../../../tests/Creole.Tests/ParseResultTests.cs) (the whitepaper switch, a separate-assembly Roslyn compile with CS8509 for a dropped case, and a reflection scan of public and protected members that expose `ParseResult<T>`), [DiagnosticsTests.cs](../../../tests/Creole.Tests/DiagnosticsTests.cs) (a CsCheck ordering property and a `Render()` snapshot). The Roslyn helper moved to [SnippetCompiler.cs](../../../tests/Creole.Tests/Utilities/SnippetCompiler.cs) and parses at the preview language version.
- Docs: [docs/concepts/errors.md](../../../docs/concepts/errors.md).
- Whitepaper, editorial only: § Results names the collection `Diagnostics`, and the sample in § Using C# 15 unions switches over the three cases with no fallback arm.
- Conventions: one type per file, and public-type folders keep the `Creole` namespace (CONTRIBUTING.md § Conventions).

Verified by an independent critic review in two rounds. Its first round found mutable errors, a missing `Failed.Deconstruct`, holes in the member scan and a weak stability oracle; all were fixed, and mutations confirmed the scan and the ordering test catch their regressions.

API detail settled here, not a design change: `Recovered<T>` and `Failed` have validating constructors instead of the sketch's positional parameters, with `Deconstruct` so patterns read the same.

Follow-ups, no new cards:

- The compiler-generated union constructors accept a null case, which makes a result equal to `default`; only nullable warnings guard it. Documented in docs/concepts/errors.md.
- When a card adds a public member that exposes `ParseResult<T>` (INT's `Parse` overloads), it must add the member to `_coveredMembers` in ParseResultTests.cs with a test that it never returns `default`.
- Re-run these tests on the GA SDK (FND-07's follow-up). All results here are from RC 2.

