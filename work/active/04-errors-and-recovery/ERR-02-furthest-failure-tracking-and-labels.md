# ERR-02: Furthest-failure tracking and labels

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Report the furthest failure with merged expectations, without allocating until a failure is actually reported.

## Scope

- Per-layer furthest failure stored as a buffer index plus an interned expectation-set handle.
- Merge failures at equal positions as [ADR-0021](../../docs/decisions/0021-how-failures-are-recorded-and-merged.md) point 1 says: expectations in recording order without duplicates, the first message, the first failure's context stack, and the longest behind-the-cursor span. Build `ParseError` lazily.
- `Named` replaces the expectations `p` recorded at its start position whenever `p` consumes nothing, on failure or success, and adds none if there were none ([ADR-0008](../../docs/decisions/0008-control-node-semantics.md) point 4). Expectations recorded there before `p` ran stay, a failure with no expectations counts, and a message is never removed ([ADR-0021](../../docs/decisions/0021-how-failures-are-recorded-and-merged.md) point 3).
- What each primitive records ([ADR-0021](../../docs/decisions/0021-how-failures-are-recorded-and-merged.md) point 6). A failed `Custom` records its `Name` as a `Label`, or else its `First` as one expectation ([ADR-0012](../../docs/decisions/0012-custom-scanner-metadata.md) point 4, as ADR-0021 refines it). `Satisfy`, `Any` and a `Custom` with neither record a failure with no expectation.
- `Lookahead` and `Not` restore the furthest failure, and a `Not` whose parser succeeds records a failure with no expectations at its start (ADR-0021 point 2).
- Dispatch records what the alternatives it skips would have expected, precomputed in lowering for each gap between candidates (ADR-0021 point 5; [ADR-0016](../../docs/decisions/0016-or-dispatches-on-each-alternatives-leading-item.md) point 4).
- A rejected filter ([ADR-0014](../../docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md) points 2 to 6). It is reported at the start of the rejected value, with a span over that value. When the predicate rejects, the furthest failure is restored to what it was before the filter's parser ran, and then the rejection is recorded. `Where(predicate, message)` records what `Fail(message)` records. Without a message, the rejected input is unexpected. A rejection counts as a failure at its start for `Named`. It lies behind the cursor, so what its error needs is captured by value, without allocating or pinning. The non-associative chain error ([ADR-0004](../../docs/decisions/0004-expression-build-operator-table-and-semantics.md) point 4), reported at a consumed operator's span, follows the same capture rule. INT-04 runs both and leaves their recording here (ADR-0014 point 7).
- Compiled subtrees ([ADR-0017](../../docs/decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md) point 6, settled by [ADR-0022](../../docs/decisions/0022-a-successful-longest-restores-the-furthest-failure.md)). A successful `Longest` restores the furthest failure, in `LongestPlan` and the compiled plan alike. So `DfaLongestPlan` records nothing on a match, and a non-match re-runs on the interpreter as now. No scan extent is exposed.
- `Context` stack (ADR-0021 point 4). A line applies only to failures past its construct's start, and captures the span of its first item by value. `ParseError.Context` becomes `IReadOnlyList<ContextLine>`, with `ContextLine(string Text, SourceSpan Start)`. That changes ERR-01's `ParseError`, its tests and docs/concepts/errors.md, and ERR-03's renderer and snapshots, which print "starting at 2:1" from `Start`. Both cards are Done; the changes are made here.
- Settled by ADR-0021, raised by the CORE-08 review: inner failures of `Not` and a successful `Lookahead`, what `Not` records when its parser succeeds, the context stack of a merged failure, `Named` and earlier expectations, and what skipped dispatch attempts contribute.
- Display ([ADR-0023](../../docs/decisions/0023-token-types-describe-their-kinds.md)). `IKinded<TKind>` gains `static virtual string Describe(TKind kind) => kind.ToString()` and `string? Detail => null`, called only when an error is built. Expected kinds show `Describe(k)`. An unexpected token shows `Describe(kind)`, then its `Detail` when present. A null or empty `Describe` falls back to `kind.ToString()`. Characters, strings, `Token(x)` items and `Case<TCase>()` (its type name) display as ADR-0023 point 5 says. `Creole.AotSmoke` gains a token that overrides both members, and the tests include a union token that does. docs/ shows the explicit implementation syntax.

## Acceptance criteria

- [ ] The whitepaper's rendered JSON error is reproduced exactly.
- [ ] Failure paths allocate nothing in benchmarks.
- [ ] A rejected filter and a chained non-associative operator are reported as ADR-0014 and ADR-0004 point 4 say, and building their errors reads no buffer item behind the cursor.
- [ ] Every point of ADR-0021 has a test. Dispatch records the same failure as running in order: INT-05's dispatch exactness tests compare recorded failures, and the CORE-08 control table gains expected sets.
- [ ] A compiled and an interpreted `Longest` leave the same furthest failure, for matches and non-matches, over `RegularShapes` and the lexer samples (ADR-0022).

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ERR-01 Error and result types](../04-errors-and-recovery/ERR-01-error-and-result-types.md)

## Unblocks

- [INT-08 Single-layer Parse API](../03-interpreter/INT-08-single-layer-parse-api.md)
- [ERR-05 Recover combinator and strategies](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)
- [ERR-06 OrInsert for missing items](../04-errors-and-recovery/ERR-06-orinsert-for-missing-items.md)
- [LAY-06 Buffer size limit](../06-layers-and-pipelines/LAY-06-buffer-size-limit.md)
- [GEN-02 Emit the core combinators](../11-source-generator/GEN-02-emit-the-core-combinators.md)
- [TOOL-03 Explain](../12-grammar-author-tooling/TOOL-03-explain.md)
- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)

## Whitepaper references

- [§ The error model](../../../whitepaper.md#the-error-model)
- [§ Failure positions](../../../whitepaper.md#failure-positions)

## Artifacts

- [ADR-0021: The first failure at a position keeps its context, predicates drop their parser's failures, labels keep messages, and dispatch records what it skips](../../docs/decisions/0021-how-failures-are-recorded-and-merged.md)
- [ADR-0022: A successful `Longest` restores the furthest failure, so a compiled `Longest` records nothing when it matches](../../docs/decisions/0022-a-successful-longest-restores-the-furthest-failure.md)
- [ADR-0023: Token types describe their kinds through a static virtual member of `IKinded`, and an unexpected token adds its own detail](../../docs/decisions/0023-token-types-describe-their-kinds.md)

## Progress notes

- 2026-10-24 (Design): Started. The scope leaves several choices open, and ADR-0012, 0014, 0016 and 0017 hand others to this card. Settle them with `/decide` before building: how `Not`, `Lookahead`, `Named`, merged context stacks, dispatch and `Satisfy` record expectations (G7), expectations on success for compiled subtrees, `Describe(TKind)` reachability, and context line positions. Next: `/decide` on the expectation rules.
- 2026-10-24 (Design): Drafted ADR-0021 (Proposed) on group A, the expectation rules: `Lookahead` and `Not` restore the furthest failure; `Named` replaces only its own expectations and keeps messages; the first failure at a position keeps its context, and lines apply only past their start; context lines become structured `ContextLine(Text, Start)`; dispatch records precomputed contributions per gap; `Satisfy` and ⊤ record no expectation. The maintainer chose the label, context, position and `Satisfy` forks. Prior art from Megaparsec 9.8.3, chumsky 0.12 and FastParse 3.1.1 is cited inline. Next: maintainer accepts, rejects or revises ADR-0021; then group B (compiled subtrees) and group C (`Describe(TKind)`).
- 2026-10-25 (Design): The maintainer accepted ADR-0021. Whitepaper § The error model, § Failure positions, § Choice and backtracking, § Interpreted engine and § Custom primitives updated. Scope now cites it, and a criterion requires a test for each point. GEN-02 updated. Next: `/decide` on group B (expectations on success for compiled subtrees, ADR-0017 point 6), then group C (`Describe(TKind)` reachability).
- 2026-10-25 (Design): Drafted ADR-0022 (Proposed) on group B, compiled subtrees. A successful `Longest` restores the furthest failure, so a compiled match records nothing, and a failed one re-runs on the interpreter as before. This was the maintainer's choice over an exact lazy re-run or annotated states. The scan's stop is only an upper bound on what the interpreter records, because `RegularTerm` models a string as atomic characters while ADR-0021 records it at its start, and because of speculative threads (finding 31). Next: maintainer accepts, rejects or revises ADR-0022; then group C (`Describe(TKind)` reachability).
- 2026-10-25 (Design): The maintainer accepted ADR-0022. Whitepaper § Longest match, § The error model and § Interpreted engine updated. The compiled-subtrees scope item cites it, and a criterion compares compiled and interpreted `Longest` failures. GEN-05 updated. Next: `/decide` on group C, how the renderer reaches `Describe(TKind)`.
- 2026-10-25 (Design): Drafted ADR-0023 (Proposed) on group C. `IKinded<TKind>` gains `static virtual string Describe(TKind kind) => kind.ToString()` and `string? Detail => null`. Expected kinds show `Describe(k)`, and unexpected tokens show `Describe(kind)` plus the detail. The maintainer chose these over a self-typed grammar, a describer passed when parsing, and enum attributes. Calling both when the error is built is the recommendation, still open for review. A scratch spike on RC 2 showed struct and union overrides, `IKinded<J>` as a type argument, and a clean Native AOT publish. `Case<TCase>()` shows its type name, which fills a gap in ADR-0021 point 6. Next: maintainer accepts, rejects or revises ADR-0023; then ERR-02 moves to Build.
- 2026-10-25 (Design): The maintainer accepted ADR-0023, with `Describe` and `Detail` called when an error is built. Updated whitepaper § Types (the `IKinded` sketch), § The error model (**Display**) and worked example A (`JTok` describes its kinds, and `ObjectValue` gains `.Context("in object")`, so the example yields the rendered error). Scope cites the record, and GEN-02 is updated. Design is complete: ADR-0021, 0022 and 0023 settle every open choice on the card.
- 2026-10-25 (Build): Started. Next: `/build` plan against the five criteria, in increments: the furthest-failure record and merge rules; control nodes, dispatch and `Longest`; filters, chain errors and context capture; `ContextLine` and the ERR-01 and ERR-03 changes; display through `IKinded`; then the whitepaper JSON error and the allocation benchmarks.
- 2026-10-25 (Build): Increment 1 done (public shapes). `ContextLine(Text, Start)` is in `src/Creole/Errors/` and rejects null or empty text, in the constructor and in `with`. `ParseError.Context` is `IReadOnlyList<ContextLine>`. `ToString()`/`Render()` print `{Text} starting at {CharStart}`. `Render(sources)` prints `= note: {Text} starting at line:column` from `Start`'s own source, and throws if that source has no text or the start is past its end. `IKinded<TKind>` gains `string? Detail => null` and `static virtual string Describe(TKind kind) => kind.ToString()`. Tests: ErrorModelTests, DiagnosticsTests and DiagnosticRenderingTests updated, plus new context-line tests; new TokenDisplayTests (struct, default and union tokens through a type parameter, and `IKinded<T>` as a type argument). `Creole.AotSmoke` gains a token that overrides both members. Full suite passes (1,239; 1,216 in Creole.Tests), and the AOT publish is warning-free and prints OK. Docs: errors.md (context lines, notes, one-line form) and primitives.md (how errors show tokens). Next: increment 2, the failure record and primitives.
