# ERR-01: Error and result types

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

The public shapes for results and errors, using C# 15 unions as decided in FND-07.

## Scope

- `ParseError` with span, layer, unexpected, expected and context.
- `Unexpected` and `Expectation` unions with their case records.
- `ParseResult<T>` as the declared union `ParseResult<T>(Parsed<T>, Recovered<T>, Failed)` ([ADR-0002](../../docs/decisions/0002-parseresult-as-a-plain-union.md)). No public API returns or exposes `default(ParseResult<T>)`.
- `Diagnostics`, an immutable collection of `ParseError` ordered by span, with `Count`, `Empty` and `Render()`.
- `Render()` is minimal: one line per error. The rich format (source names, lines and columns, gutter, carets) is [ERR-03](ERR-03-diagnostic-rendering.md).

## Acceptance criteria

- [ ] The whitepaper's `switch` on `json.Parse(text)` compiles without a default arm in an assembly that references Creole, and dropping a case is CS8509.
- [ ] No public API returns `default(ParseResult<T>)`: a test lists every public member that returns `ParseResult<T>` and fails if one has no test showing its result is never `default`.
- [ ] `Diagnostics` orders errors by span whatever order they arrive in, and keeps arrival order for equal spans (property test). `Count` and `Empty` are tested.
- [ ] `Render()` writes one line per error in span order, with layer, span, unexpected, expected and context (snapshot test).

## Blocked by

- [FND-07 Validate C# 15 features and decide `ParseResult<T>`](../01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md)
- [SPAN-01 SourceSpan, SourceId and LineMap](../05-source-spans/SPAN-01-sourcespan-sourceid-and-linemap.md)

## Unblocks

- [INT-07 Recursion depth limit](../03-interpreter/INT-07-recursion-depth-limit.md)
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
