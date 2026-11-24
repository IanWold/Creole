# ADR-0036: Trivia is the source between items' spans, stored nowhere, all of it leading, and read with a zero-width `LeadingTrivia` parser

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-11-24 |
| Cards | [SPAN-05](../../active/05-source-spans/SPAN-05-trivia-attach-mode.md), [SPAN-04](../../active/05-source-spans/SPAN-04-span-combinators.md) |
| Whitepaper | [§ Trivia](../../../whitepaper.md#trivia), [§ Using spans in grammars](../../../whitepaper.md#using-spans-in-grammars) |
| Research | A prior-art pass for this record on 2026-11-24, summarised under § Prior art below. It was not checked in as a note. |
| Supersedes | — |

## Context

§ Trivia (optional) says:

> `Layer.Tokens(item, skip, trivia: TriviaMode.Attach)` keeps skipped input as leading and trailing trivia spans on each token, in the style of Roslyn's syntax trees. That makes lossless round-tripping possible for formatters and refactoring tools. Trivia is stored as span ranges, not strings, and costs nothing when the mode is off.

SPAN-05 builds it, and its one criterion is a round-trip test. The sentence leaves six questions open:

1. **The split.** Which part of the skipped input between two tokens trails the first token, and which part leads the second?
2. **Granularity.** One range per side, or a list of pieces such as whitespace, line break and comment?
3. **Storage.** Where the ranges live, and what "costs nothing" means.
4. **End of input.** Where the input after the last token goes. That includes the whole source when it has no tokens.
5. **Later layers.** What `Where`, `Select`, `Transduce` and `Then` do with trivia.
6. **The reader.** How a grammar or a tool gets trivia. The whitepaper names no API for it.

Three facts about the code bear on these questions.

- **The skipped input is already a gap between spans.** Under span rule 2, a token's span excludes the input that `skip` consumed. A `Layer.Tokens` step starts where the previous item ended ([TokensLayer.cs](../../../src/Creole/Layers/TokensLayer.cs)). So over characters, the input skipped before token *k* is exactly the source between the end of token *k* − 1 and the start of token *k*. Storing it would repeat what the spans already say.
- **No buffer knows where its input ended.** At the filled end, `StreamBuffer<T>.SpanOf` falls back to the end of the last item ([StreamBuffer.cs](../../../src/Creole/StreamBuffer.cs)). So the source after the last token cannot be reached from the token buffer, and neither can the whole source when there are no tokens.
- **Grammars cannot read spans yet.** `ICursorSource<T>` gives a cursor items, not spans. [SPAN-04](../../active/05-source-spans/SPAN-04-span-combinators.md) adds `Position` and `Spanned()`, which need that access.

### Prior art

A research pass checked how other parsers keep trivia. Its quotes came through a fetch tool that summarises pages, so they are close paraphrases unless marked.

| System | Where trivia lives | How a gap between tokens splits | End of input |
| --- | --- | --- | --- |
| Roslyn [1][2] | On each token, as a list of typed `SyntaxTrivia` per side. `Span` excludes trivia and `FullSpan` includes it. | "A token owns any trivia after it on the same line up to the next token. Any trivia after that line is associated with the following token" [1]. The lexer ends trailing trivia just after the first end of line. Doc comments are always leading [2]. | Leading trivia of a zero-width end-of-file token [1] |
| SwiftSyntax [3][4][5] | On each token, as a list of typed `TriviaPiece`. Whitespace is stored as counts. | Trailing trivia stops just before the newline. In 2022, PR #985 changed which side comments go to, "a behavior change that swiftlint and swift-format may have to adapt to" [4]. | Leading trivia of the EOF token, in the original libSyntax design [3]. Not verified for current swift-syntax. |
| rust-analyzer (rowan) [6] | Not attached. Whitespace and comments are ordinary tokens in the tree, and "the original text can be recovered by concatenating the texts of all tokens in order". | No split. A heuristic attaches preceding comments to item nodes. | — |
| TypeScript [7] | Not stored. `getLeadingCommentRanges` and `getTrailingCommentRanges` re-scan the text on demand. A node's full start is the previous token's end. | Roslyn's split, made at query time, for comments only | — |
| Prettier [8] | Comments only, attached to AST nodes as leading, trailing or dangling | Classified by line breaks found in the original text. Blank lines are checked in the text too. | — |
| Parlot, Superpower | Discarded | — | — |

What these show:

- **No split rule is right everywhere, and changing one is costly.** The token-attached schemes all cut a gap at its first line break, but disagree on which side the break goes. Roslyn needs exceptions for doc comments and directives. When Swift changed its rule, swift-format shifted trivia around "to restore the old tree" and wrote that "a great deal of the logic in swift-format was written in terms of that behavior" [5]. rust-analyzer, Go's `CommentMap` and Prettier all attach comments to nodes by heuristic instead.
- **A cut that respects comments needs to know where the comments are.** Roslyn's and Swift's lexers know. Creole's `skip` is an opaque `Parser<TIn, Unit>`. A cut made only by looking for a line break would split a block comment that spans one.
- **Reprinting formatters get by with ranges and the text.** TypeScript stores nothing, and Prettier checks the text for line breaks. Typed pieces matter most to tools that rewrite trees: ts-morph adds its own comment nodes so that code can be inserted between comments.

Sources, as fetched on 2026-11-24:

1. Microsoft, "Use the .NET Compiler Platform SDK syntax model", learn.microsoft.com/dotnet/csharp/roslyn-sdk/work-with-syntax.
2. dotnet/roslyn, `src/Compilers/CSharp/Portable/Parser/Lexer.cs` on `main`: `LexSyntaxTrailingTrivia` and `LexSyntaxTrivia`.
3. apple/swift, `lib/Syntax/README.md` at commit ecebf707.
4. swiftlang/swift-syntax PR #985, "Change trivia attribution rule to make comments trailing trivia unless separated by a newline", merged 2022-10-20.
5. swiftlang/swift-format PR #434, "Workaround new trivia behavior in SwiftSyntax", 2022-10-21.
6. rust-lang/rust-analyzer, `docs/book/src/contributing/syntax.md` and `crates/parser/src/shortcuts.rs` on `master`.
7. microsoft/TypeScript, `src/compiler/scanner.ts` at v5.8.3, `iterateCommentRanges`.
8. Prettier documentation, "Rationale" and "Plugins".

## Options

### Where trivia comes from

**A. Stored by a `Layer.Tokens` mode (the whitepaper).** With `trivia: TriviaMode.Attach`, the layer records each token's trivia ranges in a side table that exists only when the mode is on.

- Pros: as written. The layer can compute a split while the characters are still buffered, so it works on streamed input.
- Cons: it stores what the spans already imply. Only tokens straight from a `Layer.Tokens` have trivia. `Where`, `Select`, `Transduce` and `Then` would each have to carry it forward, and a `Transduce` output made from several tokens needs a rule of its own. The mode is one more option to thread through layers and pipelines.

**B. Derived from the gaps between spans (recommended; chosen).** An item's leading trivia is the source between the previous item's end and its own start, computed when read. Nothing is stored per item. Each buffer records only where its layer's input starts and ends.

- Pros: it costs nothing whether or not anyone reads trivia: no branch in a step, and `PackedSlot` stays 32 bytes ([SPAN-02](../../active/05-source-spans/SPAN-02-out-of-band-span-storage.md)). It works at every layer with no rules per layer. Comment tokens that a `Layer.Where` drops land in the gap, which is how many grammars will want comments treated.
- Cons: above a layer that drops items, "trivia" means "source no item covers", not "what `skip` consumed". A split that needs the text, such as Roslyn's, cannot be made in a streaming parse once the text is released.

### How a gap splits

**A. Roslyn's rule, cut at the first line break.** The part of a gap up to and including its first line break trails the previous item. It is computed from the source's `LineMap`.

- Pros: it matches the whitepaper's "in the style of Roslyn" and TypeScript's comment ranges.
- Cons: it needs the line map, so it works only on in-memory input. Reading trailing trivia needs one more item of lookahead. Without knowing where comments are, it splits a block comment that spans the break, which Roslyn does not do. Doc comments get no exception.

**B. A user-supplied trailing parser.** `Layer.Tokens(item, skip, trailing: ...)` runs `skip`, then `item`, then `trailing` in each step.

- Pros: exact for any language, block comments included, and generic over `TIn`.
- Cons: another parameter. The boundary between one token's trailing trivia and the next token's leading trivia is not in the spans, so it must be stored for each token, which brings back Option A of the first fork.

**C. Leading only (recommended; chosen).** Each gap belongs whole to the item after it, and the gap after the last item is the end-of-input trivia, as on Roslyn's end-of-file token.

- Pros: it needs neither the text nor knowledge of comments. It works on streamed input. It is the one rule that is not a heuristic, so it never has to change the way Swift's did. A tool that wants trailing trivia splits a gap itself, using the text it needs anyway to read trivia.
- Cons: no trailing trivia. A refactoring tool that moves a statement must look for a same-line comment in the next gap itself.

### Granularity

**A. Typed pieces**, as Roslyn and Swift store them. `skip` becomes a repeated `Parser<TIn, TTrivia>`, and its values and spans are kept as a list for each gap.

- Pros: formatters get comments and whitespace told apart for free.
- Cons: a new type parameter on `Layer.Tokens`, a list per gap, and storage per item even under derivation.

**B. One range (recommended; chosen).** A trivia value is a `SourceSpan`.

- Pros: nothing new in `skip`'s type and nothing stored. A tool that needs pieces runs its own trivia grammar over the range, as TypeScript and Prettier re-scan.
- Cons: that tool repeats work the lexer already did.

### How a grammar or tool reads trivia

**A. A zero-width parser (recommended; chosen).** A primitive like SPAN-04's `Position` returns the gap before the next item, or before the end of the input, and consumes nothing.

- Pros: it composes with everything else, works in any layer's grammar and in streaming parses, and is one new member. It needs SPAN-04's span access in the cursor and nothing more.
- Cons: SPAN-05 waits for SPAN-04. Until LAY-05's pipelines exist, the round-trip test runs a layer through internal plumbing, as LAY-03's tests do.

**B. A listing of items on a run.** A run of a layer or pipeline returns each item with its span and leading trivia, plus the end trivia.

- Pros: the simplest thing for a formatter to call.
- Cons: it holds every item, so it does not stream. It needs LAY-05. It is a second way of reading what A gives, and can be added later as a helper over A.

## Decision

Derived gaps, leading only, one range, read with a zero-width parser. The maintainer chose each fork and accepted the record on 2026-11-24.

1. **Definition.** At any layer, an item's leading trivia is the source between the end of the item before it and its own start. Before the first item, it runs from where the layer's input starts. The end trivia runs from the end of the last item, or from where the input starts if there is none, to where the input ends.
2. **Gaps that would run backwards.** If an item starts before the previous item ends, or comes from another source, its leading trivia is zero-width at its own start. The first case arises only from a `Transduce` step that emits several derived outputs over one span (rule 2), and the second only from an embedded source.
3. **Leading only.** Each gap belongs whole to the item after it. There is no trailing trivia. The gap after the last item is the end trivia.
4. **One range.** A trivia value is a `SourceSpan`, with byte and character offsets over a byte source (§ Source spans and provenance). Creole does not classify what is in it.
5. **Nothing per item.** Trivia is computed from spans when it is read. Each stream buffer records two zero-width spans: where its layer's input starts and where it ends in the source.
   - At the root, the source sets them: 0 and the text's length for in-memory text. An embedded source gets its mapped range from SPAN-07.
   - Each layer above takes its input's.
6. **No mode.** `TriviaMode` and the `trivia:` parameter are dropped. `Layer.Tokens` keeps the two overloads it has. Trivia exists at every layer:
   - Above a `Layer.Where` that drops comment tokens, a dropped comment is in the gap before the next kept token.
   - A single-layer parse over characters has no gaps, because character *i*'s span is \[*i*, *i* + 1), so its trivia is always empty.
7. **Synthetic items.** Gaps are measured between adjacent items, synthetic ones included. A synthetic item sits at the start of the next input item (rule 3). So the gap before a run of synthetic items goes to the first of them, and the item after the run has empty trivia. In Example B, the indentation before a dedented line leads its first DEDENT. A synthetic item from `atEnd` sits at the end of the last input item, so the source after it is end trivia.
8. **Reader.** `LeadingTrivia` is a zero-width `Parser<TIn, SourceSpan>` on `Grammar<TIn>`, beside SPAN-04's `Position`.
   - It returns the leading trivia of the item at the cursor, or the end trivia at the end of the input.
   - It never fails and consumes nothing.
   - It reads the item at the cursor, so, like `Any` and `End`, it may pull one item from the layer below. It also reads the item before the cursor, which the buffer always keeps (`StreamBuffer<T>.Release`).
   - Example: `from lead in LeadingTrivia from token in Any select (lead, token)` pairs a token with its trivia. `LeadingTrivia` before `End` reads the end trivia.
   - It is named `LeadingTrivia`, not `Trivia`, because users name their own `skip` rules `Trivia`, as Example C does. A `CharGrammar` subclass with its own `Trivia` would hide the inherited member.
9. **Round trip.** Take a stream whose items are in source order and do not overlap: every `Layer.Tokens` output, and `Where`, `Select` and `Identity` over one. For each item take its leading trivia and its span, in order, then the end trivia. Those ranges cover the layer's input from start to end exactly once. Reading them from the source text reproduces it. SPAN-05's criterion tests this.
10. **Analysis.** `LeadingTrivia` is nullable, like `Position`. Repeating it alone is CRL0001's infinite loop, and the existing nullable analysis covers it.
11. **Cards.** SPAN-05 is blocked by SPAN-04, which gives the cursor span access.

Deciding reason: at a `Layer.Tokens` layer the skipped input is already exactly the gap between spans. Storing it would add a mode and per-item cost for information the buffer already has. Leading only is the one split that needs neither the text nor knowledge of comments. Every other rule is a heuristic that prior art has had to patch or change after release.

What would change this:

- A tool that needs trailing trivia from streamed input, where the text is gone before it reads the gap. That would argue for computing a cut at the `Layer.Tokens` layer and storing it.
- Users who need typed pieces from Creole and cannot run their own trivia grammar over a range.
- SPAN-04 finding that the cursor cannot read the previous item's span cheaply.

## Consequences

- **Easier.**
  - Trivia costs nothing per item, whether or not anyone reads it.
  - Every layer has trivia, with no rules for each layer and no option to pass through `Then` or pipelines.
  - The generated engine implements one more zero-width primitive.
- **Harder.**
  - There is no trailing trivia, so a formatter that wants Roslyn's attachment splits gaps itself.
  - A streaming parse can read trivia ranges but not their text, unless the caller keeps the text. Diagnostics have the same limit (§ Streaming and async input).
  - Above a layer that drops items, trivia includes the dropped items' text.
- **Ruled out.** Storing trivia per item, `TriviaMode`, and typed trivia pieces in Creole's core. A later helper could split a gap Roslyn's way over the text, or classify its pieces with a user's grammar, without changing what is stored.
- **Watch.**
  - SPAN-05's round-trip test, including an input with no items.
  - A model test over random gaps, synthetic items and segment boundaries.
  - Engine agreement on `LeadingTrivia` once the generator exists (§ Equivalence).

## Changes made

- Whitepaper:
  - § Trivia (optional) is now § Trivia and describes this record, which it links. The `trivia:` parameter, `TriviaMode`, trailing trivia and the comparison with Roslyn are gone.
  - § Using spans in grammars: a bullet for `LeadingTrivia`.
- Cards:
  - [SPAN-05](../../active/05-source-spans/SPAN-05-trivia-attach-mode.md): retitled "Trivia from the gaps between spans". Its scope follows this record. Its criteria keep the round-trip test, add an input with no tokens, and add tests for trivia above a `Layer.Where` and before synthetic items, and for `PackedSlot` staying 32 bytes. It is now blocked by SPAN-04 and back to Not started until SPAN-04 is done.
  - [SPAN-04](../../active/05-source-spans/SPAN-04-span-combinators.md): unblocks SPAN-05.
  - [SPAN-02](../../active/05-source-spans/SPAN-02-out-of-band-span-storage.md), [LAY-03](../../active/06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md): SPAN-05's new title in their Unblocks.
  - [work/README.md](../../README.md): SPAN-05's title and blockers. It moves from wave 5 to wave 6 and leaves "In progress".
