# GEN-09: Native AOT and trimming validation

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 3 — Staging |
| Size | S |
| Status | Not started |

## Summary

Both engines must be safe under Native AOT and trimming (G8).

## Scope

- Publish AOT sample apps for every worked example on both engines.
- Verify LINQ-query grammars under AOT (interpreted expressions).
- No runtime IL emission or reflection on the hot path.

## Acceptance criteria

- [ ] Zero trim and AOT warnings; AOT apps produce the same results as JIT runs.

## Blocked by

- [FND-03 Continuous integration](../01-foundations/FND-03-continuous-integration.md)
- [CORE-10 Bind and LINQ query syntax](../02-core-combinators/CORE-10-bind-and-linq-query-syntax.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)

## Unblocks

- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)

## Whitepaper references

- [§ Design goals and non-goals](../../../whitepaper.md#design-goals-and-non-goals)
