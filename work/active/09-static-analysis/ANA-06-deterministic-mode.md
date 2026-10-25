# ANA-06: Deterministic mode

| | |
| --- | --- |
| Epic | [Static analysis](../../README.md#ana--static-analysis) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

`Grammar.Deterministic(p)` asserts LL(1) and buys a linear-time guarantee.

## Scope

- Check rules 1 to 6, including the same-mode rule for alternatives and hoisting of shared mode changes; report CRL0006.
- Compile every `Or` to a jump table; take no pinning bookmarks.
- Allow `Recover`, `OrInsert`, `Cut` and non-dispatch mode changes.

## Acceptance criteria

- [ ] Example A's `Document` passes; a grammar with `Atomic` inside fails with CRL0006.
- [ ] Parse time is linear in tokens on generated inputs.

## Blocked by

- [INT-05 Interpreter lowering rewrites](../../complete/03-interpreter/INT-05-interpreter-lowering-rewrites.md)
- [LAY-02 Marks and retention](../06-layers-and-pipelines/LAY-02-marks-and-retention.md)
- [MODE-01 Mode channels and Layer.Modal](../07-mode-channels/MODE-01-mode-channels-and-layermodal.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [ANA-02 Analysis report and Debug enforcement](../09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md)

## Unblocks

- [RSLN-03 Build-time diagnostic reporting](../10-roslyn-analyzers/RSLN-03-build-time-diagnostic-reporting.md)
- [GEN-05 Generated DFAs, scans and jump tables](../11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md)
- [REL-01 Worked-example samples](../14-release/REL-01-worked-example-samples.md)

## Whitepaper references

- [§ Deterministic mode](../../../whitepaper.md#deterministic-mode)
