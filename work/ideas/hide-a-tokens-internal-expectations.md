# Hide a token's internal expectations

Added 2026-10-25.

**What:** A combinator, say `p.Hidden()` or `p.Lexeme()`, that forgets every failure recorded inside `p` when `p` succeeds. A single-layer grammar wraps each token in it, so an error lists the tokens that could come next, not the characters inside the token just read or the whitespace after it. The engine can already do this: a successful `Longest` restores the furthest failure ([ADR-0022](../docs/decisions/0022-a-successful-longest-restores-the-furthest-failure.md)), so `Longest(p, p)` behaves like the proposed combinator, in an awkward form.

**Why it might matter:** In the [Phase 0 review](../docs/research/2026-10-25-phase-0-review.md) (finding 23), a fresh user's single-layer grammars showed "whitespace", `[0-9]` and `'.'` in nearly every error. One example is `max(1, 2`, which reports `expected [0-9], '.', whitespace, '^', '*', '/', '+', '-', ',' or ')'`. `Named` cannot remove them, because it relabels only failures where its parser consumed nothing ([ADR-0021](../docs/decisions/0021-how-failures-are-recorded-and-merged.md)). Two-layer grammars avoid the problem, because the lexer's internals never reach the parser layer's errors, but Phase 0 has only one layer. Megaparsec's `hidden` is said to serve this purpose (unverified, from the reviewer).

**Touches:** [§ The error model](../../whitepaper.md#the-error-model), ADR-0021, ADR-0022, [CORE-08](../active/02-core-combinators/CORE-08-control-nodes.md) (`Named` and `Context`), docs/concepts/control.md, [GEN-02](../active/11-source-generator/GEN-02-emit-the-core-combinators.md) (the generator would record identically).

**First question:** does forgetting a successful token's internal failures lose hints users want? After `1.`, for instance, "expected digit" is useful. That is the trade ADR-0022 accepted for `Longest`. If it is acceptable, should the combinator also hide the trailing whitespace's expectation, or only the token's?
