# ANA-02: Analysis report and Debug enforcement

| | |
| --- | --- |
| Epic | [Static analysis](../../README.md#ana--static-analysis) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Analysis results are available in tests and enforced on first use in Debug builds.

## Scope

- `grammar.Analyze()` returning a report with errors, warnings and infos.
- On first use in Debug builds, errors throw `GrammarException` naming the rule path. Names come from GEN-01's name table and from `Rec` ([ADR-0009](../../docs/decisions/0009-where-rule-names-come-from.md)); unnamed nodes are left out of the path. Before GEN-01, only `Rec`s are named, so Example A's path shows `Value` but not `Document` or `ObjectValue`.

## Acceptance criteria

- [ ] `Assert.Empty(JsonGrammar.Document.Analyze().Errors)` works in a test.

## Blocked by

- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)

## Unblocks

- [ANA-03 Loop and recursion checks](../09-static-analysis/ANA-03-loop-and-recursion-checks.md)
- [ANA-04 Choice checks](../09-static-analysis/ANA-04-choice-checks.md)
- [ANA-05 Informational and hygiene checks](../09-static-analysis/ANA-05-informational-and-hygiene-checks.md)
- [ANA-06 Deterministic mode](../09-static-analysis/ANA-06-deterministic-mode.md)
- [ANA-07 Linear maximal munch](../09-static-analysis/ANA-07-linear-maximal-munch.md)
- [ANA-08 Custom scanner contract checks](../09-static-analysis/ANA-08-custom-scanner-contract-checks.md)

## Whitepaper references

- [§ Where analysis runs](../../../whitepaper.md#where-analysis-runs)
