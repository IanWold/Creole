# SPAN-03: Synthetic items

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Status | Done |

## Summary

Items produced without consuming input, such as INDENT, DEDENT or an inserted semicolon.

## Scope

- An item is synthetic exactly when its span is zero-width. Nothing stores a flag, and readers test `span.Length == 0` ([ADR-0034](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md) points 1–5).
- LAY-03 gives synthetic `Transduce` outputs their zero-width spans, since its loop computes every span ([ADR-0032](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md) points 7 and 8). This card pins the rule across every layer kind and documents it.
- Example B's DEDENT placement is checked by [LAY-07](../06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md), which builds Example B (ADR-0034 point 7).

## Acceptance criteria

- [x] Tests show an item's span is zero-width exactly when it covers no source text, through `Tokens`, `Transduce`, `Where`, `Select` and `Then`, including a token built from synthetic items at two positions and a derived output of a step that consumed only synthetic input.
- [x] An error at a synthetic output placed after a newline renders on the line of the next token.

## Blocked by

- [SPAN-02 Out-of-band span storage](../05-source-spans/SPAN-02-out-of-band-span-storage.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [ERR-06 OrInsert for missing items](../04-errors-and-recovery/ERR-06-orinsert-for-missing-items.md)
- [LAY-07 Three-layer demo and bounded-memory check](../06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md)

## Whitepaper references

- [§ Span rules](../../../whitepaper.md#span-rules)

## Artifacts

- [ADR-0032: `Emit<T>` is a list of outputs, each derived or synthetic, not a union](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md)
- [ADR-0034: A synthetic item is one whose span is zero-width, and no flag is stored](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md)

## Progress notes

- 2026-11-24 (Design): Started. LAY-03 already places synthetic `Transduce` outputs (ADR-0032 points 7 and 8), so what is left is the `IsSynthetic` flag, and neither the whitepaper nor ADR-0032 says where it is stored, how a downstream layer or parser reads it, or whether it carries through `Where`, `Select` and a derived item built only from synthetic input. The `StreamSlot` has no flag and `SourceSpan` has none, and a zero-width span does not imply synthetic. Separately, the criterion needs Example B's indentation pass, which is stateful: that needs LAY-04, which is not a blocker, and LAY-07, which builds the pass, is blocked by this card. Decisions to make: the flag's storage and read API; its propagation; where the Example B check lives. Next: `/decide`.
- 2026-11-24 (Design): Drafted ADR-0034 (Proposed). The maintainer chose: an item is synthetic exactly when its span is zero-width, and nothing stores a flag; the Example B criterion moves to LAY-07, which builds Example B and already scopes the check, and SPAN-03 gets a criterion pinning the rule across `Tokens`, `Transduce`, `Where`, `Select` and `Then` with a stateless pass. Evidence: Python 3.13.7's tokenizer, run locally, puts DEDENT zero-width at the first token of the dedented line and stores no flag. A prior-art check found that Roslyn (`IsMissing`) and tree-sitter (`ts_node_is_missing`) store a flag only for recovery-inserted tokens and have zero-width tokens without it; the ADR names that as what would change the decision. Next: the maintainer accepts, rejects or revises ADR-0034; on acceptance, edit § Span rules rule 3 and § Recovery, update SPAN-03, LAY-07, ERR-06, SPAN-06 and SPAN-07, then move to Build.
- 2026-11-24 (Design → Build): The maintainer accepted ADR-0034. Whitepaper § Span rules rule 3 and § Recovery now say an item is synthetic exactly when its span is zero-width, and no flag is stored. Scope and criteria follow ADR-0034 point 7; the Example B criterion moved to LAY-07. ERR-06, SPAN-06 and SPAN-07 gained scope notes. Next: `/build`. Plan the tests for both criteria (a stateless pass such as `[newline, Emit.Synthetic(dedent)]` over a lexer that skips indentation covers the second), and the docs in `docs/concepts/spans.md` and `layers.md`.
- 2026-11-24 (Build): Plan approved: one increment of tests and docs, with no runtime code (ADR-0034 points 2 and 3). Done. `SyntheticItemTests` (5), with exact spans: a chain of lexer, layout pass, `Select` and `Where` joined with `Then`, where every item is zero-width exactly when it is synthetic (criterion 1); a token over two synthetic items at one position is zero-width; a token over synthetic `;` items at 1 and 2 on `a\n\nb` spans 1..2; a derived output of a step that consumed only a synthetic item is zero-width (criterion 1's two cases); an error at a `<dedent>` emitted after a newline renders at `2:3` on `a = 1\n  b = 2\n` (criterion 2). Real tokens being wider than zero was already covered by `LayerTests.ATokenSpanCoversItsOwnInputAndNoSkippedInput`, and marked and `atEnd` outputs being zero-width by `TransduceTests.OutputsTakeTheirSpansStepStartsAndGroupIndices`. Mutations each failed tests: `Select` giving its items zero-width spans (1), and a trailing synthetic placed at the end of its step (4, including the rendering test). Docs: `spans.md` gains § Synthetic items and links it from the zero-width bullet; `layers.md` shows the rendered error and links the rule; the docs index mentions synthetic items. XML docs: `Emitted<T>` says the mark is not kept, and `SourceSpan` says a zero-width item is synthetic. `dotnet build` 0 warnings; `dotnet test` 1,668 passed. Both criteria appear met; not ticked until `/complete`. Next: maintainer review, then `/complete SPAN-03`.
- 2026-11-24 (Done): Verified both criteria on `b42a3cd`: `SyntheticItemTests` 5 passed, the two cited tests passed, `dotnet build` 0 warnings, `dotnet test` 1,668 passed. Card closed.

## Outcome

- **Definition** ([ADR-0034](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md)): an item is synthetic exactly when its span is zero-width, so it covers no source text. Nothing stores a flag. `Emitted<T>.IsSynthetic` only tells `Layer.Transduce` where to place an output's span. Whitepaper § Span rules rule 3 and § Recovery say so. No runtime code changed.
- **Tests.** [SyntheticItemTests.cs](../../../tests/Creole.Tests/SyntheticItemTests.cs), 5 tests with exact spans. Two mutations each failed tests: `Select` giving zero-width spans, and a trailing synthetic placed at the end of its step.
- **Docs.** [Source spans § Synthetic items](../../../docs/concepts/spans.md#synthetic-items) gives the rule and both edge cases. [Layers § `Layer.Transduce`](../../../docs/concepts/layers.md#layertransduce) shows an error at a DEDENT rendered on the next line. The docs index mentions synthetic items. XML docs on `Emitted<T>` and `SourceSpan` state the rule.
- **Deviations.** Both are in ADR-0034, which revises ADR-0032 point 11:
  - The card's scope promised a stored `IsSynthetic` flag that downstream layers read. It would have held what the span already says, and disagreed with it in two cases.
  - The Example B criterion moved to [LAY-07](../06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md). Example B's indentation pass needs LAY-04's layer state, and LAY-07 builds it.
- **Evidence.**

  | Criterion | Evidence |
  | --- | --- |
  | 1. Zero-width exactly when no source text is covered, through every layer kind and both cases | `SyntheticItemTests.AnItemIsZeroWidthExactlyWhenItIsSyntheticThroughEveryLayer` (lexer, `Transduce`, `Select`, `Where`, `Then`), `ATokenOverSyntheticItemsAtOnePositionIsZeroWidth`, `ATokenOverSyntheticItemsAtTwoPositionsCoversTheTextBetween` (1..2 on `a\n\nb`), `ADerivedOutputOverOnlySyntheticInputIsZeroWidth`. Real tokens are wider than zero by `LayerTests.ATokenSpanCoversItsOwnInputAndNoSkippedInput`; marked and `atEnd` outputs are zero-width by `TransduceTests.OutputsTakeTheirSpansStepStartsAndGroupIndices` |
  | 2. An error at a synthetic output after a newline renders on the next token's line | `SyntheticItemTests.AnErrorAtASyntheticOutputAfterANewlineShowsOnTheNextLine`: `expected statement` at `2:3`, caret under `b` |

- **Follow-ups.**
  - LAY-07 checks DEDENT placement on Example B.
  - ERR-06, or editor tooling, may need to tell recovery-inserted items from layout items, as Roslyn's `IsMissing` and tree-sitter's `ts_node_is_missing` do. That needs a stored flag and a new decision.
  - SPAN-06 and SPAN-07 test that real input never gets a zero-width span.
