# GEN-11: Runtime support surface for generated code

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Decide the public surface that generated code calls to agree with the interpreter, and how it reaches the interpreter for a subtree it cannot generate, before the generator emits code against either.

## Scope

- Generated code is compiled into the user's assembly, so it can call only public API. Everything it needs to agree with the interpreter (G7) is internal today:
  - the cursor's cut flag, depth counting, `Reset` and `Since`;
  - failure recording: `Fail`, `FailAll`, `Reject`, `Relabel`, the save and restore marks, and context frames;
  - `Expected`, `FailureMark`, `DepthExceededException`, `StagedParse` and `Staging`.
- Generated code runs with no failure log as well as with one, and behaves alike in both, because a parse's first run has none ([ADR-0027](../../docs/decisions/0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md)).

  The public `Cursor<T>` offers only `Advance`, `TryPeek`, `TryRead`, `Available`, `IsAtEnd` and `Position`.
- Choose the granularity: raw cursor hooks, or coarser helpers hidden from IntelliSense (`[EditorBrowsable(Never)]`). Whatever is chosen becomes 1.0 contract at REL-04. Exposing today's internals as they stand would freeze [ADR-0021](../../docs/decisions/0021-how-failures-are-recorded-and-merged.md)'s recording protocol.
- The surface carries the per-layer parse state LAY-08 lists.
- Fallback: decide how generated code calls the interpreter for a subtree inside a rule field (§ Source-generated engine). `Staging` is keyed by the rule field's node, and a subtree is reachable only through the internal `Children`. If fallback goes by child path, `Children` order becomes contract.
- Registration accepts one node held by two rule fields. Today `Staging.Register` throws on a second registration, so a field that aliases another rule (`Name = A.Identifier`) would fail under staging but work when interpreted.
- The whitepaper's generated sample calls `c.PeekKind()`, which does not exist. Align the sample with the surface chosen.
- From the [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md), finding 12.

## Acceptance criteria

- [ ] An accepted decision record fixes the support surface and the fallback mechanism.
- [ ] A hand-written stand-in for generated code covering one rule of Example A is compiled in an assembly with no `InternalsVisibleTo`. On a fixed corpus it records the same failures, cut flag and depth as the interpreter.

## Blocked by

- [LAY-08 Per-layer parse state](../06-layers-and-pipelines/LAY-08-per-layer-parse-state.md)

## Unblocks

- [GEN-01 Generator skeleton and registration](GEN-01-generator-skeleton-and-registration.md)
- [GEN-02 Emit the core combinators](GEN-02-emit-the-core-combinators.md)

## Whitepaper references

- [§ Source-generated engine](../../../whitepaper.md#source-generated-engine)
- [§ Equivalence](../../../whitepaper.md#equivalence)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
