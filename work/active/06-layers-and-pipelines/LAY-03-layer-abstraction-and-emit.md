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
