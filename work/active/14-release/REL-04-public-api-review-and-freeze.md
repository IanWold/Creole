# REL-04: Public API review and freeze

| | |
| --- | --- |
| Epic | [Samples, docs and 1.0 release](../../README.md#rel--samples-docs-and-10-release) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

Review and freeze the 1.0 public surface.

## Scope

- API review of every public type, including union usage in results and errors.
- Public API baseline tracking (for example PublicApiAnalyzers).

## Acceptance criteria

- [ ] API baseline committed; breaking changes require explicit approval afterwards.

## Blocked by

- [FND-07 Validate C# 15 features and decide `ParseResult<T>`](../01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md)
- [ERR-07 Lexer recovery and cascade suppression](../04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md)
- [SPAN-07 Embedded languages](../05-source-spans/SPAN-07-embedded-languages.md)
- [STRM-03 Streaming diagnostics](../08-streaming/STRM-03-streaming-diagnostics.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)
- [TOOL-01 Creole.Testing assertions](../12-grammar-author-tooling/TOOL-01-creoletesting-assertions.md)

## Unblocks

- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)

## Whitepaper references

- [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions)
