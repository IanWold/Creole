# LAY-03: Layer abstraction and Emit

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

Layers turn a stream of one type into a stream of another using ordinary combinators.

## Scope

- `Layer<TIn, TOut>`; `Layer.Identity<T>()`; associative composition.
- `Layer.Tokens(item, skip)` loop; reject nullable items at run time until ANA-03 adds CRL0003.
- The derived-item span rule (§ Span rules, rule 2): an item's span runs from the first input item its item parser consumed to the end of the last, and input consumed by `skip` is excluded. Moved here from [SPAN-02](../05-source-spans/SPAN-02-out-of-band-span-storage.md), since this loop computes it.
- `Layer.Transduce(initialState, step, atEnd)` and `Emit<T>` as [ADR-0032](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md) defines them: a list of outputs, each derived or synthetic, not a union. A step that succeeds without consuming is rejected at run time, as a `Layer.Tokens` item is. Record `groupIndex` for every output of a step, synthetic ones included. The state reaches `atEnd` unchanged; LAY-04 lets the step read and change it.
- Spans of `Transduce` outputs: rule 2 for derived outputs, each getting the whole step span; rule 3 for synthetic ones, at the start of the next input item as ADR-0032 point 7 defines it. SPAN-03 adds the `IsSynthetic` flag downstream layers read.
- `Layer.Where` and `Layer.Select` item functions.
- Guarantee 4: a layer's own backtracking is invisible downstream.

## Acceptance criteria

- [ ] A lexer composed with a stateless layout pass, a `Transduce` that turns each newline token into a synthetic `;`, is usable as a single `Layer<char, Token>`.
- [ ] A token's span never includes leading whitespace.
- [ ] Building and reading an `Emit<T>` of zero or one output allocates nothing.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [LAY-01 StreamBuffer and pulling cursor](../06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md)

## Unblocks

- [ERR-07 Lexer recovery and cascade suppression](../04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md)
- [SPAN-03 Synthetic items](../05-source-spans/SPAN-03-synthetic-items.md)
- [SPAN-04 Span combinators](../05-source-spans/SPAN-04-span-combinators.md)
- [SPAN-05 Trivia attach mode](../05-source-spans/SPAN-05-trivia-attach-mode.md)
- [SPAN-06 Byte sources and the decoding layer](../05-source-spans/SPAN-06-byte-sources-and-the-decoding-layer.md)
- [LAY-04 Layer-local state](../06-layers-and-pipelines/LAY-04-layer-local-state.md)
- [LAY-05 Pipeline API and sources](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)
- [MODE-01 Mode channels and Layer.Modal](../07-mode-channels/MODE-01-mode-channels-and-layermodal.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [ANA-03 Loop and recursion checks](../09-static-analysis/ANA-03-loop-and-recursion-checks.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)

## Whitepaper references

- [§ Layers, streams and pipelines](../../../whitepaper.md#layers-streams-and-pipelines)
- [§ Defining layers](../../../whitepaper.md#defining-layers)
- [§ Guarantees](../../../whitepaper.md#guarantees)
- [§ Span rules](../../../whitepaper.md#span-rules)

## Artifacts

- [ADR-0002: `ParseResult<T>` is a plain C# 15 union](../../docs/decisions/0002-parseresult-as-a-plain-union.md)
- [ADR-0032: `Emit<T>` is a list of outputs, each derived or synthetic, not a union](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md)

## Progress notes

- 2026-10-27 (Design): Started. The card leaves `Emit<T>`'s shape open (ADR-0002 § Consequences), and settling it changes § Where unions are not used, so a decision record comes before the build. Decisions to make: `Emit<T>`'s cases and how a step marks synthetic outputs and gives each output its span; how `Transduce`'s step reads state before LAY-04 adds `GetState` and `Scan`, which criterion 1's layout pass needs; layers as data, the composition method and what runs a layer before LAY-05's pipeline API; how a failed item parser surfaces downstream and under which layer name. Next: `/decide`.
- 2026-10-27 (Design): Drafted ADR-0032 (Proposed). The maintainer chose: `Emit<T>` is a list struct, not a union, whose outputs are each derived or synthetic, built with collection expressions; a synthetic output sits at the start of the next input item; LAY-03's criterion 1 uses a stateless `Transduce` pass and LAY-07 builds Example B's indentation pass. An RC 2 spike (scratchpad, recorded in the ADR) showed collection expressions mixing items and `Emit.Synthetic(...)` compile, zero or one output allocates nothing, and interface-typed items need `Emit.One`. Decisions 4 (layers as data, composition, an internal runner) and 5 (a failed item parser and layer names) are left for the build plan. Next: the maintainer accepts, rejects or revises ADR-0032; on acceptance, update the whitepaper and the six cards, then move to Build.
- 2026-10-27 (Build): ADR-0032 accepted. Whitepaper updated (§ Where unions are not used, § Defining layers, § Span rules, CRL0003, Example B) and SPAN-03, LAY-04, LAY-07, ANA-03 and MODE-04 changed to match. Scope and criterion 1 follow the record; a criterion pins zero allocation for zero or one output. Open for the build plan: layers as data (one sealed class per layer kind, per ADR-0001's pattern), the composition method and its flattening, an internal runner for tests before LAY-05, what a failed item parser does downstream, and how layers are named for `ParseError.Layer`. Next: `/build LAY-03`.
- 2026-10-27 (Build): Plan approved: three increments (`Emit<T>`; the layer model with `Tokens`, `Where`, `Select`, `Identity` and `Then`; `Transduce`). Calls: layers run through an internal `Open(StreamBuffer<TIn>, …)` until LAY-05; `a.Then(b)` composes, and composing with `Identity` returns the other layer; `skip` is an optional `Parser<TIn, Unit>` that must succeed; a step that consumes nothing throws `GrammarException`; a failed step ends the parse with its layer's furthest failure, or its depth error, carried by an internal exception for LAY-05 to catch; `Tokens` and `Transduce` take an optional `name`, default `lexer` and `layout`. Increment 1 done: `Emit<T>` (`EmitOfT.cs`), `Emitted<T>` and the `Emit` class (`One`, `Synthetic`, and a hidden `Create` for collection expressions) as ADR-0032 points 1–4 define them. `EmitTests` (10): building with collection expressions, conversions, spreads, `Emit.One` for an interface-typed item, CS0029 for that item's conversion, equality and hash codes, `ToString`, and no allocation for zero or one output (criterion 3). Two mutations each failed a test: `Create` copying a single output into an array, and equality ignoring the mark. Docs: new `docs/concepts/layers.md` with the `Emit` section, added to the docs index. `dotnet build` 0 warnings; `dotnet test` 1,606 passed; AOT smoke publishes and prints OK. Criterion 3 appears met; not ticked until `/complete`. Next: increment 2, the layer model and `Layer.Tokens`.
- 2026-10-27 (Build): Increment 2 done. `Layer<TIn, TOut>` (`LayerOfTInTOut.cs`), an abstract class with an internal constructor, one internal `Open(StreamBuffer<TIn>, ParseOptions, bool recordFailures)` that returns the `IUpstream<TOut>` filling the buffer above, and `Then`. The `Layer` class makes `Tokens`, `Where`, `Select` and `Identity`. Each kind is a sealed class in `src/Creole/Layers/` with its upstream as a `file` type. `Then` returns the other layer when either side is `Identity`, and otherwise a `ComposedLayer` that puts a `StreamBuffer` between the two. One change to the approved plan: `skip` is a second `Tokens` overload, not a nullable parameter, so a null `skip`, such as a field declared later, throws `GrammarException` instead of being silently ignored. Each `Tokens` step runs on a cursor of its own and keeps one failure log for the whole run. A failed `skip` or item throws the internal `LayerFailedException`, which carries the layer's furthest failure across its steps, or the depth error. An item that consumes nothing throws `GrammarException` (`NullableRepetition.LayerItem`). The depth and nothing-recorded errors moved from `Parsing` into `ParseErrors`, so `Parsing` and layers build them the same way; `Parsing`'s tests pass unchanged. Tests: `LayerTests` (17) and the `LayerRun` helper. Criterion 2 is a CsCheck property over random tokens and whitespace with segment sizes 1–4 (500 cases), checking each token's span and step start. The other tests cover spans and step starts, input that is all skip, items read back to back, a failed item (pinned message), a failed `skip`, a custom name, the depth error, an item that consumes nothing, the failure log spanning steps, `Where`, `Select`, associativity, identity as the unit, identity run on its own, null arguments, and guarantee 4: a parser that rewinds over a backtracking lexer never runs it again. Mutations each failed tests: the span including `skip` (4), `stepStart` after `skip` (2), `Then` not returning the layer for a left `Identity` (1), a new failure log per step (2), and the depth limit ignored (1). Docs: `layers.md` covers making layers, `Tokens`, its errors, `Where` and `Select`, and `Then`; `spans.md` gains Spans of a layer's items; the docs index is updated. `dotnet build` 0 warnings; `dotnet test` 1,623 passed; AOT smoke publishes and prints OK. Criterion 2 appears met; not ticked until `/complete`. Next: increment 3, `Layer.Transduce`.
- 2026-10-27 (Build): Increment 3 done. `Layer.Transduce(initialState, step, atEnd = null, name = "layout")` and `TransduceLayer` in `src/Creole/Layers/`, as ADR-0032 points 5–10 define it. Derived outputs get the step's span; a synthetic output with no derived output before it in its step is zero-width at the step's first item; one after a derived output is zero-width at the first item after the step, which the step peeks for, or at the end of the input. `atEnd` runs once at the end with the state, which is still `initialState`, and its outputs are zero-width there whatever their mark. `groupIndex` counts every output of a step. A step that consumes nothing throws `GrammarException` (`NullableRepetition.LayerStep`), and a failed step throws `LayerFailedException` named `layout` or the given name. Tests: `TransduceTests` (7). They cover criterion 1: a lexer composed with a pass that turns newlines into synthetic `;` and adds a synthetic `<end>` is a `Layer<char, string>`, a parser reading its buffer parses the statements, and every span is checked. Also: a table of spans, step starts and group indices covering every placement rule and an empty emit; a failed step (pinned message); a custom name; a step that consumes nothing; no `atEnd`; null arguments. Mutations each failed tests: a trailing synthetic placed at the end of the step (1), a leading synthetic given the step's span (2), `groupIndex` always 0 (1), `atEnd` never run (2), and derived outputs never noted (1). Docs: `layers.md` gains a `Layer.Transduce` section with the placement table and a row in Making layers; `spans.md` and the docs index mention `Transduce`. `dotnet build` 0 warnings; `dotnet test` 1,630 passed; AOT smoke publishes and prints OK. All three criteria appear met; none ticked until `/complete`. Next: maintainer review, then `/complete LAY-03`.
