# Full-word combinator names

Added 2026-10-19. From the CORE-07 Outcome.

**What:** The maintainer plans to rename the public combinators to consistent full-word names. The built API mixes styles: `SepBy`, `SepBy1` and `SepEndBy` sit beside `AtLeastOnce` and `SkipAtLeastOnce`; `Seq` and `CIString` are abbreviated; the operator table uses `InfixL`, `InfixR` and `InfixN`. Most of the short names come from the whitepaper, which follows Pidgin and Parsec.

**Why it might matter:** Every card that merges adds more names and more tests and docs that use them, so a rename gets more expensive with each one. REL-04 freezes the public API. Full words read better for users who don't know Parsec, but the whitepaper deliberately follows Pidgin's vocabulary ([§ Comparison with existing .NET libraries](../../whitepaper.md#comparison-with-existing-net-libraries)), and Pidgin users would lose the one-to-one mapping.

**Touches:** [§ Authoring ergonomics](../../whitepaper.md#authoring-ergonomics), [§ Repetition and structure](../../whitepaper.md#repetition-and-structure), [§ Expressions](../../whitepaper.md#expressions), the worked examples, [ADR-0004](../docs/decisions/0004-expression-build-operator-table-and-semantics.md), and the docs pages for repetition, sequencing, primitives and expressions. The rename itself is a `/refactor`.

**First question:** Which names change, and to what? `/decide` should list each current name with its proposed replacement, say whether Pidgin's names stay as documented aliases or only in the docs mapping, and settle it before CORE-10 and CORE-12 add more names.
