# INT-09: Cheaper automaton construction

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Lowering builds a compiled `Longest`'s automaton in full, and for a large lexer that is costly. INT-06 measured the 138-rule C#-like lexer at about 107 ms to lower, allocating about 516 MB of short-lived memory. This card cuts that cost within eager construction first, and turns to lazy construction only if that falls short.

## Scope

- **Find where the cost goes.** Profile `CommitAutomaton.Build` on the C#-like lexer. The suspects:
  - the string key built for every configuration the construction reaches;
  - the signature strings built to drop duplicate paths;
  - copying every alternative's candidate list on every step.
- **Cut it within eager construction.** Possible means: hash states structurally, without building strings; compute each path's signature once; copy fewer lists.
- **Only if that cannot meet the criteria:** propose lazy construction with `/decide`. It would build states on demand while scanning, which ends plan immutability (ADR-0017 point 3), and it needs thread-safe table growth, a cache cap, and a fallback when the cache churns.
- **Who pays the cost.** Grammars the source generator stages get their tables at build time (GEN-05). So this cost falls on grammars built at run time or left unstaged.
- **Timing.** No gate needs this card; BENCH-01 measures parsing, not lowering. Pick it up when a grammar built at run time shows the cost matters, or once GEN-05 shows how much of it staging removes.

## Acceptance criteria

- [ ] Lowering the C#-like lexer (`DfaLowerCSharp` in `LoweringBenchmarks`) allocates at least 10× less than before this card, and takes no longer, median of three runs.
- [ ] Tokenizing (`DfaJsonLexer`, `DfaCSharpLexer`) stays within 5% of its speed before this card, median of three runs.
- [ ] The automaton, lowering and allocation tests pass unchanged.
- [ ] If construction becomes lazy, a decision record covering what replaces plan immutability is accepted before the build starts.

## Blocked by

- [INT-06 DFA compiler for regular subtrees](../03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)
