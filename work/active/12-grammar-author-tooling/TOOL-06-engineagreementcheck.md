# TOOL-06: EngineAgreement.Check

| | |
| --- | --- |
| Epic | [Grammar-author tooling](../../README.md#tool--grammar-author-tooling) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

Give users the same agreement check the library's CI uses.

## Scope

- `Creole.Testing.EngineAgreement.Check(grammar, inputs)`.
- Engines agree only on grammars whose functions follow [ADR-0026](../../docs/decisions/0026-engines-may-skip-unused-values-and-repeat-user-functions.md). A mismatch on a user's grammar can mean an impure function, so the report says so.
- Compare against the interpreter with and without its lowering rewrites, as GEN-08 does. `LoweringOptions` is internal, so `Creole.Testing` needs a way to choose ([Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 13).

## Acceptance criteria

- [ ] Users can assert agreement for their own grammars in one line.

## Blocked by

- [GEN-08 Differential engine-agreement tests](../11-source-generator/GEN-08-differential-engine-agreement-tests.md)

## Unblocks

- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)

## Whitepaper references

- [§ Equivalence](../../../whitepaper.md#equivalence)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
