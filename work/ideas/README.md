# Ideas

Thoughts not yet ready to be cards or research notes. See [CONTRIBUTING.md § Ideas](../../CONTRIBUTING.md#ideas) for how ideas are captured, promoted and dropped.

## Ideas

| Idea | Added | Touches | Summary |
| --- | --- | --- | --- |
| [Union tokens and `IKinded`](union-tokens-and-ikinded.md) | 2026-10-19 | § Where unions are not used, CORE-03, ANA-01 | Research spike: union tokens must also implement `IKinded<TKind>` to use `TokenGrammar`, which the whitepaper does not seem to intend. |
| [Full-word combinator names](full-word-combinator-names.md) | 2026-10-19 | § Authoring ergonomics, § Repetition and structure, § Expressions, ADR-0004 | Rename the public combinators to consistent full words (`SepBy1` beside `AtLeastOnce`) before more cards add names; needs `/decide`. |
| [`Match` over non-boxing unions](match-over-non-boxing-unions.md) | 2026-10-22 | § Design choices, ADR-0007, CORE-05, GEN-02 | `Match` reads `IUnion.Value`, so `Match` over `Maybe<int>` boxes on every parse (measured in INT-05), contradicting ADR-0007 and § Design choices. |

## Dropped

- None yet.
