# GEN-10: Measure the interpreter fallback rate

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 3 — Staging |
| Size | S |
| Status | Not started |

## Summary

Risk mitigation: if the generator's authoring rules feel restrictive, users avoid staging.

## Scope

- Measure how often CRL0301 fires on the samples and on a few real-world grammars ported from other libraries.
- File follow-up items for common unreadable patterns.

## Acceptance criteria

- [ ] A short report with the fallback rate and the top unreadable patterns.

## Blocked by

- [GEN-04 Interpreter fallback and CRL0301](../11-source-generator/GEN-04-interpreter-fallback-and-crl0301.md)
- [REL-01 Worked-example samples](../14-release/REL-01-worked-example-samples.md)

## Unblocks

- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)

## Whitepaper references

- [§ Risks](../../../whitepaper.md#risks)
