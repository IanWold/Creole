# LAY-07: Three-layer demo and bounded-memory check

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Building |
| Gate | Phase 1 gate |

## Summary

The Phase 1 gate: the indentation-sensitive language from Example B runs, and memory stays bounded.

## Scope

- Implement Example B (lexer, layout layer, parser) as a test and sample. Its indentation pass is the first stateful `Transduce`; LAY-03 built only a stateless one ([ADR-0032](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md) point 10).
- Memory test for G2 on Example B and a JSON-lines stream generated at 10 MB, 100 MB and 1 GB: retained heap sampled after forced full blocking GCs at fixed item intervals, minus a warm-up baseline, one process per size.
- Check DEDENT span placement in error messages. This check moved here from SPAN-03 ([ADR-0034](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md) point 7).

## Acceptance criteria

- [ ] Peak retained heap at 1 GB is at most 1.5× the peak at 10 MB for both inputs ([ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md)).
- [ ] Example B's errors at a DEDENT, and its "expected statement" errors after a block, point at the dedented line.

## Blocked by

- [SPAN-03 Synthetic items](../05-source-spans/SPAN-03-synthetic-items.md)
- [LAY-04 Layer-local state](../06-layers-and-pipelines/LAY-04-layer-local-state.md)
- [LAY-05 Pipeline API and sources](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)
- [LAY-06 Buffer size limit](../06-layers-and-pipelines/LAY-06-buffer-size-limit.md)

## Unblocks

- [REL-01 Worked-example samples](../14-release/REL-01-worked-example-samples.md)

## Whitepaper references

- [§ B. An indentation-sensitive language in three layers](../../../whitepaper.md#b-an-indentation-sensitive-language-in-three-layers)
- [§ Guarantees](../../../whitepaper.md#guarantees)
- [§ Roadmap](../../../whitepaper.md#roadmap)

## Artifacts

- [ADR-0032: `Emit<T>` is a list of outputs, each derived or synthetic, not a union](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md)
- [ADR-0034: A synthetic item is one whose span is zero-width, and no flag is stored](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md)

## Progress notes

- 2026-11-25 (Build): Started. Blockers SPAN-03, LAY-04, LAY-05 and LAY-06 are Done and § Risks has nothing open, so Build. Approach: Example B (lexer, stateful `Transduce` layout pass over an immutable indent stack, parser) as a test and a sample; a separate memory-gate console run per size over generated streamed input for Example B and JSON lines; the DEDENT error-placement check. To settle in the build plan: Example B's `Root` builds a `Module` whose statement list grows with the input, so the memory run needs a root whose result does not grow (`ParseMany` over top-level statements, or a single streamed `Parse` that skips its statements); where the harness lives and how it generates input; whether recovery is needed for the inconsistent-dedent `Error` token before ERR-05 exists. Next: `/build LAY-07`.
- 2026-11-25 (Build): Plan drafted, awaiting approval. Increment 1: Example B as `samples/Creole.Samples.Indentation` with `WorkedExampleBTests` (criterion 2). Increment 2: `benchmarks/Creole.MemoryGate` with a generating `TextReader` that samples the heap every N records, and `scripts/memory-gate.py` running each input and size in its own process (criterion 1). Open forks put to the maintainer: Example B's memory run as one streamed `Parse` whose root skips its statements (recommended) or `ParseMany` over top-level statements; criterion 2's wording, since under Example B's grammar an "expected statement" error never sits on a DEDENT (the error that does is a missing block, "expected indent"). Next: maintainer approves or revises the plan.
- 2026-11-25 (Build): Plan approved with both recommended forks: the Example B memory run is one streamed `Parse` whose root skips its statements, and criterion 2 is reworded. Increment 1 done. Example B is the sample `samples/Creole.Samples.Indentation` (in `Creole.slnx`): `MiniLexer`, `Layout.Pass` (a `Transduce` whose state is the immutable `IndentLevels`), `MiniGrammar`, and a `Script` that prints in a standard form. CA1716 and CA1711 rejected the names `If`, `While`, `Module` and `IndentStack`, so they are `IfStatement`, `WhileStatement`, `Script` and `IndentLevels`. An inconsistent dedent emits DEDENTs, then a synthetic `Error` token that the parser rejects until ERR-05 adds recovery. Criterion 2 reworded to "errors at a DEDENT, and 'expected statement' errors after a block", because an "expected statement" error never sits on a DEDENT: `Suite` consumes it. The § B sentence got the matching editorial fix. `WorkedExampleBTests` (9 tests): the pipeline over a string and a `TextReader`; the spans of INDENT, DEDENT, blank lines and `atEnd`; a CsCheck property, in which 2,000 random programs with random indentation and blank lines parse to their standard form at segment sizes 1, 2, 3 and 1,024; and the criterion 2 renderings for a missing block (at the DEDENT, 3:1), an inconsistent dedent (3:3) and a stray `)` (3:1). Mutations: a derived DEDENT fails the missing-block test and two span tests; one DEDENT per newline fails the property and both pipeline tests. Docs: layer-state.md § A layout pass, a link from layers.md, samples/README.md. `dotnet build` with 0 warnings; `dotnet test` with 1,997 passed and 0 failed. Next: maintainer review and commit, then increment 2, the memory gate.
