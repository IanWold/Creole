# LAY-07: Three-layer demo and bounded-memory check

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Done |
| Gate | Phase 1 gate |

## Summary

The Phase 1 gate: the indentation-sensitive language from Example B runs, and memory stays bounded.

## Scope

- Implement Example B (lexer, layout layer, parser) as a test and sample. Its indentation pass is the first stateful `Transduce`; LAY-03 built only a stateless one ([ADR-0032](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md) point 10).
- Memory test for G2 on Example B and a JSON-lines stream generated at 10 MB, 100 MB and 1 GB: retained heap sampled after forced full blocking GCs at fixed item intervals, minus a warm-up baseline, one process per size.
- Check DEDENT span placement in error messages. This check moved here from SPAN-03 ([ADR-0034](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md) point 7).

## Acceptance criteria

- [x] Peak retained heap at 1 GB is at most 1.5× the peak at 10 MB for both inputs ([ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md)).
- [x] Example B's errors at a DEDENT, and its "expected statement" errors after a block, point at the dedented line, or at the end of the input for the DEDENTs that close blocks there.

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
- 2026-11-26 (Build): Increment 2 done: the memory gate. `benchmarks/Creole.MemoryGate` (in `Creole.slnx`) generates each input as it is read (`GeneratedReader`, one record at a time from a fixed seed). It parses Example B as one streamed `Parse` with root `MiniGrammar.Statement.SkipMany().Before(End)`, and JSON lines as `ParseMany` over the benchmarks' token lexer and `CreoleTokenJson.Value` (made public). Every 1,000 records it forces a full blocking compacting GC and samples the retained heap, less a baseline taken after a 1 MB warm-up run. `scripts/memory-gate.py` runs each input and size in its own process and checks the threshold. The first measure, `GC.GetTotalMemory` read after the GC, failed JSON lines once at 1.90× (18,088 bytes at 1 GB against 9,528 at 10 MB) while the test suite ran beside it. Some samples were exactly 8,192 bytes high: an allocation context another thread took between the GC and the read. Logging both measures on one loaded run gave 8 such samples with `GetTotalMemory` and none with `GCMemoryInfo`, so the harness now reads `HeapSizeBytes − FragmentedBytes` from `GC.GetGCMemoryInfo(GCKind.FullBlocking)`. That counts the large object heap: holding a 1 MB array adds 1,003,456 bytes. Gate run on efbd7ab plus this increment, uncommitted: Example B retains 77,128, 78,072 and 78,480 bytes at 10 MB, 100 MB and 1 GB (1.02×); JSON lines 9,528, 9,592 and 9,592 (1.01×); both pass. Beside the test suite, JSON lines at 1 GB still measures 9,592. Mutation: a `StreamBuffer.Release` that releases nothing fails both inputs from 2 MB to 20 MB (14.24× and 15.60×). `MemoryGateTests` (7): both inputs parse at 100,000 characters with pinned hashes, reading one character at a time gives the same text, and the reader ends and samples where it says. Docs: benchmarks/README.md § Memory gate and CLAUDE.md's command table. Every criterion now appears met. Next: maintainer review and commit, optionally `python3 scripts/memory-gate.py` from the clean commit, then `/complete LAY-07`.
- 2026-11-26 (Done): Verified for completion. A gate run on 8314d9a, with a clean tree, gave the same values as the increment 2 run. An independent review (critic) found both criteria met and nothing blocking. It ran the "release nothing" mutation again and got the same failures, and placed 13 more error cases on the dedented line. It also found that DEDENTs closing blocks still open at the end of the input sit at the end of the input, as span rule 3 requires, and no line follows them. Criterion 2 now says so, and `ADedentThatClosesABlockAtTheEndOfTheInputReportsThere` pins it (2:10 for `while a:\n    if b:`). § B and layer-state.md got the same editorial addition. The review also found three wrong claims, now corrected: the JSON-lines input is ASCII but for an occasional `é`, which was meant to be the escape `\u00e9`; `RetainedBytes` is growth above the warmed-up baseline, not all the parse holds; and the README's note on load rests on one run.

## Outcome

Example B runs in three layers, and the Phase 1 memory gate passes for both of its inputs.

- **Example B.** The sample [Creole.Samples.Indentation](../../../samples/Creole.Samples.Indentation/) holds `MiniLexer`, `Layout.Pass` and `MiniGrammar`. `Layout.Pass` is the first stateful `Transduce`, a pass over the immutable `IndentLevels`. `Script` prints the program in a standard form. Analyzers rejected the whitepaper sketch's names `If`, `While`, `Module` and `IndentStack` (CA1716, CA1711), so they are `IfStatement`, `WhileStatement`, `Script` and `IndentLevels`. Docs: [layer-state.md § A layout pass](../../../docs/concepts/layer-state.md#a-layout-pass), [layers.md](../../../docs/concepts/layers.md) and [samples/README.md](../../../samples/README.md).
- **DEDENT placement (criterion 2).** `WorkedExampleBTests` holds 10 tests:
  - Rendered errors: a missing block at its DEDENT (3:1); an inconsistent dedent (3:3); a stray token after a block (3:1); and a DEDENT at the end of the input (2:10).
  - Spans: INDENT and DEDENT sit at the first token of their line, and blank lines belong to the newline before them.
  - The pipeline parses a program over a string and over a `TextReader`.
  - A CsCheck property parses 2,000 random programs at segment sizes 1, 2, 3 and 1,024.
- **Memory gate (criterion 1).** The harness is [benchmarks/Creole.MemoryGate](../../../benchmarks/Creole.MemoryGate/), run by [scripts/memory-gate.py](../../../scripts/memory-gate.py) and described in [benchmarks/README.md § Memory gate](../../../benchmarks/README.md#memory-gate). It generates each input as it reads it. It parses Example B in one streamed `Parse`, and JSON lines with `ParseMany`. Every 1,000 records it forces a full blocking compacting GC, and it subtracts a baseline taken after a 1 MB warm-up run. Each input and size runs in its own process. The retained heap is `HeapSizeBytes − FragmentedBytes` from `GC.GetGCMemoryInfo(GCKind.FullBlocking)`. `GC.GetTotalMemory` was not used: on a busy machine it counted an 8 KB allocation context, taken after the GC, in 8 of 3,598 samples, and made JSON lines fail at 1.90×. Across that run's other samples the two measures agree. `MemoryGateTests` pins both inputs and checks that they parse.
- **Whitepaper.** § B got editorial fixes: which errors sit on the dedented line, and where DEDENTs at the end of the input sit.

Gate evidence (Phase 1 memory, [ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md): peak retained heap at 1 GB ≤ 1.5× the peak at 10 MB), on commit 8314d9a with a clean tree:

| Input | 10 MB | 100 MB | 1 GB | 1 GB ÷ 10 MB |
| --- | --- | --- | --- | --- |
| Example B | 77,128 B | 78,072 B | 78,480 B | 1.02× |
| JSON lines | 9,528 B | 9,592 B | 9,592 B | 1.01× |

Values are growth above the baseline. A `StreamBuffer.Release` that releases nothing fails both inputs from 2 MB to 20 MB, at 14.24× and 15.60×, so the gate can fail. The rest of the Phase 1 gate, "Three-layer demo runs", is Example B.

Deviations: none from the design. Criterion 2 was reworded twice, as the progress notes say. The sample's parser has no recovery yet: the Error token of an inconsistent dedent ends the parse, though § B says the parser's recovery skips it. Recovery is [ERR-05](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md).

Follow-ups, proposed and not yet on any card:

- **Measure the gate again at the end of Phase 1.** This gate was measured before [MODE-02](../07-mode-channels/MODE-02-invalidation-rule.md) to MODE-06 and [STRM-02](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md) to STRM-04. Some of those change what buffers keep, or add state to each diagnostic. Running `scripts/memory-gate.py` could be a criterion of the last Phase 1 card.
- **Limits of the Example B sample, for [REL-01](../14-release/REL-01-worked-example-samples.md):**
  - A blank line at the start of the input is rejected.
  - A last line without a newline is rejected.
  - Indentation on the first line is accepted, and then the same indentation on the second line is an unexpected indent.
  - There is no recovery from the inconsistent-dedent Error token. REL-01 is not blocked by ERR-05.
- **The JSON-lines `é`.** `JsonLinesInput._escapes` holds a literal `é` where the escape `\u00e9` was meant. Restoring the escape changes the pinned hash and needs a new gate run.
