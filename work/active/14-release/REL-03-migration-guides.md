# REL-03: Migration guides

| | |
| --- | --- |
| Epic | [Samples, docs and 1.0 release](../../README.md#rel--samples-docs-and-10-release) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

Guides for Pidgin and Sprache users.

## Scope

- Pidgin: `Try` to `Atomic`, `Map` to `Seq(...).Select`, grammar base classes.
- Pidgin's `Maybe<T>` against Creole's union `Maybe<T>`: `HasValue ? Value : x` becomes a switch over `T` and `Nothing`, `TryGetValue` or `GetValueOrDefault` ([ADR-0007](../../docs/decisions/0007-maybe-as-a-non-boxing-union.md)).
- Sprache: commit-by-default and CRL0102, side by side with the benchmark grammars.

## Acceptance criteria

- [ ] Both guides published.

## Blocked by

- [BENCH-02 Full benchmark suite](../13-benchmarks/BENCH-02-full-benchmark-suite.md)

## Unblocks

- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)

## Whitepaper references

- [§ Comparison with existing .NET libraries](../../../whitepaper.md#comparison-with-existing-net-libraries)
