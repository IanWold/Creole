# Ideas

Thoughts not yet ready to be cards or research notes. See [CONTRIBUTING.md § Ideas](../../CONTRIBUTING.md#ideas) for how ideas are captured, promoted and dropped.

## Ideas

| Idea | Added | Touches | Summary |
| --- | --- | --- | --- |
| [Union tokens and `IKinded`](union-tokens-and-ikinded.md) | 2026-10-19 | § Where unions are not used, CORE-03, ANA-01 | Research spike: union tokens must also implement `IKinded<TKind>` to use `TokenGrammar`, which the whitepaper does not seem to intend. |
| [Full-word combinator names](full-word-combinator-names.md) | 2026-10-19 | § Authoring ergonomics, § Repetition and structure, § Expressions, ADR-0004 | Rename the public combinators to consistent full words (`SepBy1` beside `AtLeastOnce`) before more cards add names; needs `/decide`. |
| [`Match` over non-boxing unions](match-over-non-boxing-unions.md) | 2026-10-22 | § Design choices, ADR-0007, CORE-05, GEN-02 | `Match` reads `IUnion.Value`, so `Match` over `Maybe<int>` boxes on every parse (measured in INT-05), contradicting ADR-0007 and § Design choices. |
| [Commit versus regex reading diagnostic](commit-versus-regex-reading-diagnostic.md) | 2026-10-23 | § Static analysis, ANA-04, INT-06, CRL0102 | Warn when a regular rule matches differently under commit semantics than as a regex, such as the Parsec-style decimal that fails on `1.x`. |
| [`SearchValues`-accelerated self-loops](searchvalues-accelerated-self-loops.md) | 2026-10-24 | § Interpreted engine, INT-06, INT-09, GEN-05 | Skip a self-looping automaton state's run with `IndexOfAnyExcept`; might make non-`Longest` regular subtrees worth compiling (INT-06's trial ran 0.59×) and speed up compiled lexers. |
| [Hide a token's internal expectations](hide-a-tokens-internal-expectations.md) | 2026-10-25 | § The error model, ADR-0021, ADR-0022, CORE-08, GEN-02 | A combinator that forgets the failures recorded inside a token that succeeded, so single-layer errors stop listing whitespace and a token's own characters; `Longest(p, p)` already does it, awkwardly. |

## Dropped

- None yet.
