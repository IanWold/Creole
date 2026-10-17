# LAY-03: Layer abstraction and Emit

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Layers turn a stream of one type into a stream of another using ordinary combinators.

## Scope

- `Layer<TIn, TOut>`; `Layer.Identity<T>()`; associative composition.
- `Layer.Tokens(item, skip)` loop; reject nullable items at run time until ANA-03 adds CRL0003.
- `Layer.Transduce(initialState, step, atEnd)` with `Emit<T>` as a hand-written `[Union]` struct; record `groupIndex` for multi-output steps.
- `Layer.Where` and `Layer.Select` item functions.
- Guarantee 4: a layer's own backtracking is invisible downstream.

## Acceptance criteria

- [ ] A lexer composed with a layout pass is usable as a single `Layer<char, Token>`.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [LAY-01 StreamBuffer and pulling cursor](../06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md)

## Unblocks

- [ERR-07 Lexer recovery and cascade suppression](../04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md)
- [SPAN-03 Synthetic items](../05-source-spans/SPAN-03-synthetic-items.md)
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
