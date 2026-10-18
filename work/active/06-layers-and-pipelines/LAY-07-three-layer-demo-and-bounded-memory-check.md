# LAY-07: Three-layer demo and bounded-memory check

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Not started |
| Gate | Phase 1 gate |

## Summary

The Phase 1 gate: the indentation-sensitive language from Example B runs, and memory stays bounded.

## Scope

- Implement Example B (lexer, layout layer, parser) as a test and sample.
- Memory test for G2 on Example B and a JSON-lines stream generated at 10 MB, 100 MB and 1 GB: retained heap sampled after forced full blocking GCs at fixed item intervals, minus a warm-up baseline, one process per size.
- Check DEDENT span placement in error messages.

## Acceptance criteria

- [ ] Peak retained heap at 1 GB is at most 1.5× the peak at 10 MB for both inputs ([ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md)).

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
