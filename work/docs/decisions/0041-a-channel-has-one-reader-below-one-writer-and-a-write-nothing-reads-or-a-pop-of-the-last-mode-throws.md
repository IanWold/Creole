# ADR-0041: A mode channel has one `Layer.Modal` reader below one writing layer, a write nothing reads in a pipeline or a pop of the last mode throws, and each `ParseMany` item starts in the initial mode

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-11-25 |
| Cards | [MODE-01](../../active/07-mode-channels/MODE-01-mode-channels-and-layermodal.md), [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md), [MODE-05](../../active/07-mode-channels/MODE-05-mode-payloads.md) |
| Whitepaper | [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels), [§ Defining layers](../../../whitepaper.md#defining-layers), [§ 2. Layer-local state](../../../whitepaper.md#2-layer-local-state) |
| Research | MODE-01's overload spike and a reading of ANTLR's lexer source, recorded below (2026-11-25, scratchpad, not checked in) |
| Supersedes | — |

## Context

§ 3. Mode channels settles the API's shape. A `ModeChannel<TMode>` is a typed key with an initial mode. `Layer.Modal(channel, selector, skip:)` reads it and calls its selector once per distinct mode, caching the result for the life of the layer. `SetMode`, `PushMode` and `PopMode` are zero-width parsers that write it. [ADR-0030](0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 1 puts "the mode log and mode stack of each channel the layer writes" in the writing layer's parse state. [ADR-0037](0037-lexers-take-an-initial-state-parsers-set-theirs-and-a-mistyped-read-throws.md) point 2 gives `Layer.Modal` a form that takes `initialState` first, and asks MODE-01 to check its overloads for CS0121.

Five behaviours that users will see are not settled:

1. **A write that nothing reads.** A channel "connects an upstream layer to any layer downstream of it". Nothing says what a write does when no layer below the writer reads the channel. That happens when the parser uses a different `ModeChannel` instance from the lexer, when the write sits in or below the `Layer.Modal`, or when a parser runs on its own over text or tokens.
2. **Popping the last mode.** Nothing says what `PopMode` does when the stack holds one mode.
3. **Several layers on one channel.** ADR-0030 point 1 says "If MODE-01 lets two layers write one channel, each layer keeps the entries it made". Nothing says whether two `Layer.Modal`s may read one channel.
4. **`ParseMany` items.** ADR-0030 point 1 says mode logs last "one item under `ParseMany`". It does not say whether the next item starts in the initial mode or in the mode the last item left.
5. **Skip by mode.** The card and § Defining layers give `Layer.Modal` one `skip` for all modes. § Feedback between layers lists "element content vs attributes in JSX-like syntax" as a use of mode channels. In element text, whitespace is content. A skip that eats `//` comments would also eat `//x.com</a>` in `<a>http://x.com</a>`.

### The spike

MODE-01 checked overload resolution on SDK 11.0.100, `LangVersion` 15. It used stand-in types shaped like the three forms below, and a `Longest` subclass of `Parser` in one switch arm, as Example C has.

| Overloads | Call | Binds to |
| --- | --- | --- |
| `(channel, selector, name)`, `(channel, selector, skip, name)`, `(initialState, channel, selector, skip, name)` | `Modal(Modes, mode => …)`, `(…, "x")`, `(…, name: "x")` | `(channel, selector, name)` |
| The same | `Modal(Modes, mode => …, Skip)`, `(…, skip: Skip)`, `(…, Skip, "x")`, and a selector written as a conditional | `(channel, selector, skip, name)` |
| The same | `Modal(0, Modes, …, Skip)`, `Modal(ImmutableStack<int>.Empty, Modes, …, Skip, name: "x")`, and all arguments named | `(initialState, channel, selector, skip, name)` |

No call is ambiguous. A `ModeChannel<TMode>` cannot be confused with a selector or a skip parser, so the three forms do not overlap as `Layer.Tokens`'s did.

Found while drafting: § Defining layers sample 4 switches over an enum with no `_` arm. On the same SDK, that switch warns CS8524 ("does not handle some values of its input type … involving an unnamed enum value"). Example C has the arm.

### Prior art

ANTLR's lexer modes are set by the lexer itself, not by a parser, but they use the same stack. In the Java runtime, `mode(m)` sets the current mode, `pushMode(m)` pushes the current mode and then sets `m`, and `popMode()` on an empty stack throws `EmptyStackException` ([`Lexer.java`](https://github.com/antlr/antlr4/blob/master/runtime/Java/src/org/antlr/v4/runtime/Lexer.java)). The C# runtime throws `InvalidOperationException` there ([`Lexer.cs`](https://github.com/antlr/antlr4/blob/master/runtime/CSharp/src/Lexer.cs)). `reset()` clears the stack and returns to the default mode. Both files were read on master on 2026-11-25.

## Options

### Fork A: a write that no layer below reads

#### A1. Throw in a pipeline; a parser on its own keeps a stack nothing reads (recommended)

In a pipeline, a write to a channel that no layer below the writer reads throws `GrammarException` when it runs. It names the writing layer and the channel. A parser run on its own over text or tokens has no layers to steer. Its writes change its own stack, which nothing reads.

- Pros: the common mistake, a second `ModeChannel` instance, fails on the first write with a message that names it. A parser that steers its lexer can still be tested over a list of tokens. The check adds no work beyond finding the channel, which every write does: layers open from the bottom up ([`ComposedLayer.Open`](../../../src/Creole/Layers/ComposedLayer.cs)), so when a layer opens, the channels read below it are already known.
- Cons: two rules, one for pipelines and one for a parser on its own. A mistake shows only when a parse reaches the write.

#### A2. Always throw

As A1, and a parser run on its own throws too.

- Pros: one rule. It is as strict as ADR-0037 point 4 is for state.
- Cons: a parser that steers its lexer can run only inside its pipeline. Over tokens that are already lexed, a mode change has nothing to steer, so throwing there protects nothing.

#### A3. Never throw

Every writer keeps its stack, read or not.

- Pros: the least to build.
- Cons: a second channel instance lexes silently under the initial mode forever. The user sees only a parse error, such as an unexpected `/`, far from the cause. ADR-0037 rejected the same trade for state (its 3C).

#### A4. Check when the pipeline is built

Walk each layer's grammar for mode nodes when `Then` builds the pipeline, following `Rec`. Throw if a channel written has no reader below.

- Pros: the mistake fails before any input is read.
- Cons: a `Bind` continuation can return a mode node that no walk sees, so A1 or A3 is still needed at run time. ADR-0037 rejected an open-time walk (its 3B) and left build-time checks to analysis.

### Fork B: `PopMode` when the stack holds one mode

#### B1. Throw `GrammarException` (recommended)

- Pros: a parser pairs its pushes and pops through the grammar's structure, such as `Seq(Kind(LBrace), PushMode(Modes, M), Body, Kind(RBrace), PopMode(Modes))`. Popping the last mode means some path pops more than it pushed, which is a mistake in the grammar, like reading missing state. ANTLR throws here in both runtimes.
- Cons: a grammar that pairs them through the input instead, such as a flat loop that pops on every `}`, throws on input with a stray `}`. It must guard the pop itself.

#### B2. Fail without consuming

- Pros: unbalanced input gives a parse error, and an `Or` can try another alternative.
- Cons: the failure has no expectation of its own, so the error reads only "unexpected `}`" unless a `Named` labels it. It hides a grammar mistake behind an input error.

### Fork C: several layers on one channel

#### C1. One reader and one writing layer per channel (recommended)

A chain that holds two `Layer.Modal`s on the same channel throws `GrammarException` when `Then` builds it. The first layer to write a channel in a run becomes its writer. A write from another layer throws `GrammarException`.

- Pros: one producer to invalidate (MODE-02, MODE-04) and one mode log to undo (MODE-03). The reader check needs only the layer chain, not the grammar. Lifting either limit later breaks no grammar that works today.
- Cons: a layout layer and the parser cannot both steer one lexer. No known grammar needs that.

#### C2. One reader, several writing layers

Each writing layer keeps the log entries it made (ADR-0030 point 1). The stack is shared.

- Pros: a layout layer and the parser could both steer the lexer.
- Cons: undoing one layer's entries restores a stack that another layer may have changed since. MODE-03 would need a rule for that.

#### C3. Several readers and writers

- Pros: the most general.
- Cons: a change invalidates several producers' buffers. MODE-02 and MODE-04 grow, with no grammar known to need it.

### Fork D: where each `ParseMany` item starts

#### D1. In the channel's initial mode (recommended)

The parser's channels start at their initial modes for each item, with an empty mode log. If the last item left a channel in another mode, the reset is a mode change at the new item's first index.

- Pros: it follows ADR-0030 point 1, which puts the mode stack in the writer's parse state, and the parser's state starts afresh with each item (ADR-0037 point 3). Each item is lexed as it would be at the start of a parse.
- Cons: the last item may have looked past its end, as a `Many` does to stop, and lexed the next item's first token under its final mode. Once MODE-02 exists, the reset re-lexes that token.

#### D2. In the mode the last item left

- Pros: no change at item boundaries.
- Cons: an item's tokens depend on how the last item's parse ended. The stack would outlive the log that can undo it. It would also behave differently from the parser's own state.

### Fork E: one `skip` for every mode

#### E1. One shared `skip`, for now (recommended)

As the card and § Defining layers say.

- Pros: three overloads, matching `Layer.Tokens`. Each mode's cached entry is one item parser.
- Cons: in a mode where text is content, the grammar works around the shared skip. A JSX-like lexer skips whitespace only. It lexes comments as tokens in code mode and drops them with `Layer.Where`. The parser reads whitespace skipped in text mode with `LeadingTrivia` ([ADR-0036](0036-trivia-is-the-source-between-items-read-with-a-zero-width-parser.md)).

#### E2. The selector chooses each mode's item and skip

Add overloads whose selector returns both, such as `mode => mode switch { Code => (CodeToken, Trivia), Text => (TextChunk, Return(new Unit())) }`.

- Pros: JSX-like modes need no workaround.
- Cons: five `Modal` overloads instead of three, with no card that needs them yet. They can be added later without breaking any call.

## Decision

Options **A1, B1, C1, D1 and E1**. The maintainer chose forks A, B, C and E, and accepted the record with D1 on 2026-11-25.

1. **Channels.** `ModeChannel<TMode>` is a class whose identity is the channel. It holds the initial mode. Modes compare with `EqualityComparer<TMode>.Default`. A channel is data, like a parser: it holds no mode at run time, and one channel can serve any number of parses at once.
2. **The stack.** Each channel has a stack of modes per run, which starts as the initial mode alone. `SetMode(channel, mode)` replaces the top. `PushMode(channel, mode)` pushes. `PopMode(channel)` pops, and throws `GrammarException` when the stack holds one mode (B1). Following ADR-0030 point 1, the stack and its log belong to the layer that writes the channel.
3. **Reader and writer.** A channel has at most one reader in a chain, the `Layer.Modal` that names it. A chain built with `Then` that holds two throws `GrammarException`. A channel has at most one writing layer in a run, the first to write it. A write from another layer throws `GrammarException` (C1). A channel that nothing writes stays in its initial mode.
4. **A write nothing reads.** In a pipeline, a write to a channel that no layer below the writer reads throws `GrammarException` when it runs. That covers a second channel instance, and a write in or below the reading layer. A parser run on its own changes its own stack, which nothing reads (A1). The message names the writing layer and the channel's mode type.
5. **Reading.** At the start of each step, `Layer.Modal` reads the top of its channel's stack and runs that mode's item parser after `skip`. `skip` is shared by every mode (E1). Otherwise a step runs as a `Layer.Tokens` step does: spans, failures, the empty-item check, and `stateBefore`.
6. **The selector.** It runs at most once per distinct mode for the life of the layer, including when parses run at once on several threads. Its result is cached on the layer, so each mode's item parser is planned once. A selector that returns null throws `GrammarException` naming the mode. One that throws ends the parse with its exception, as user code does.
7. **Forms.** `Layer.Modal<TIn, TOut, TMode>(ModeChannel<TMode> channel, Func<TMode, Parser<TIn, TOut>> selector, string name = "lexer")`, the same with `Parser<TIn, Unit> skip` after `selector`, and `Layer.Modal<TIn, TOut, TMode, TState>(TState initialState, ModeChannel<TMode> channel, Func<TMode, Parser<TIn, TOut>> selector, Parser<TIn, Unit> skip, string name = "lexer")`. The stateful form follows ADR-0037 points 2 to 5, as `Layer.Tokens`'s does. Each grammar-taking parameter has a `[CallerArgumentExpression]` partner, as `Layer.Tokens`'s have. The spike shows no CS0121.
8. **Mode nodes.** `SetMode`, `PushMode` and `PopMode` are members of `Grammar<TIn>`, as `SetState` is. They take the channel first and produce `Unit`.
9. **`ParseMany`.** The parser's channels start each item at their initial modes, with empty logs. A layer that writes a channel runs once per parse, so its stack lasts the parse (D1). Under [ADR-0027](0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md) and [ADR-0038](0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md), a second run starts every channel afresh with the layers it reopens.
10. **What MODE-01 builds alone.** A change affects the steps that run after it. Discarding items already lexed (MODE-02) and undoing changes on a rewind (MODE-03) come with those cards. MODE-01's tests change mode only where the next item is not yet lexed. Its documentation describes only what it builds.

Deciding reasons:

- **A1:** it catches the likely mistake with a clear message, and still lets a parser that steers its lexer run over a token list, where throwing would protect nothing.
- **B1:** pushes and pops pair through the grammar's structure, so popping the last mode is a grammar mistake. ANTLR treats it the same way.
- **C1:** MODE-02 to MODE-04 each handle one producer and one log. The limits can be lifted later without breaking a grammar.
- **D1:** it is the rule ADR-0030 already implies for the writer's parse state.
- **E1:** no card needs a skip per mode, the workaround exists, and E2 can be added without breaking a call.

What would change this:

- A grammar where a layout layer and the parser must both steer one lexer argues for C2.
- Grammars that pop on input, with users hitting point 2's exception on stray closers, argue for B2.
- A JSX-like or template grammar on the roadmap, or users finding the `LeadingTrivia` workaround awkward, argues for E2.
- Analysis that reads pipelines (ANA, RSLN-01) could report point 4's mistake at build time. That would make A4's walk unnecessary at run time.

## Consequences

- **Easier.** The mistakes most likely in practice each throw with a message: a second channel instance, a write below its reader, a pop of the last mode, and two readers or writers. MODE-02 to MODE-04 handle one producer and one log per channel.
- **Harder.** A JSX-like lexer works around the shared skip. A grammar that pops on input guards its pops. A layout layer and the parser cannot share a channel.
- **Ruled out for now.** Two `Layer.Modal`s on one channel, two writing layers, and a skip per mode. Each can be added later without breaking a grammar.
- **For MODE-02.** Point 9's reset at a `ParseMany` item's start is a mode change. It discards what the last item lexed past its end under another mode.
- **For MODE-05.** Point 6 caches per distinct mode value. A mode case that carries data gets one entry per value, so payload modes need a bound or a cache keyed by case.
- **Watch.** `Layer.Modal` reads the stack once per step. LAY-07's benchmarks, or MODE-01's own, compare a `Layer.Modal` with one mode against the same `Layer.Tokens`.

## Changes made

- Whitepaper:
  - [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels): a new **Stacks, readers and writers** paragraph after the selector paragraph gives points 2 to 4 and 9. It says `skip` is shared by every mode, and that a mode where text is content reads skipped text back with `LeadingTrivia`. It links this record.
  - [§ Defining layers](../../../whitepaper.md#defining-layers): sample 4 has a `_` arm, as Example C does (editorial, CS8524). The paragraph after the samples says `Layer.Modal` loops as `Layer.Tokens` does, with one `skip` for every mode, and takes a state first.
  - [§ 2. Layer-local state](../../../whitepaper.md#2-layer-local-state): `Layer.Modal(initialState, channel, selector, skip)` joins the forms of a lexer with state.
  - [§ Running pipelines](../../../whitepaper.md#running-pipelines): each `ParseMany` item starts the parser's mode channels in their initial modes.
  - Checked and unchanged: § Using C# 15 unions (its row on lexer modes), § Deterministic mode rule 6, § Reusing parser results and § Limits under incremental re-parsing (entry mode stacks), and Example C, which already has a `_` arm and one channel instance.
- Cards:
  - [MODE-01](../../active/07-mode-channels/MODE-01-mode-channels-and-layermodal.md): scope follows points 1 to 10, plus the benchmark comparison under § Consequences. A second criterion covers the `GrammarException` cases of points 2 to 4, a parser that writes a channel parsing on its own over tokens, and point 9. § Defining layers joins its whitepaper references. Moved to Building.
  - [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md): a scope line for point 9's reset at a `ParseMany` item's start.
  - [MODE-05](../../active/07-mode-channels/MODE-05-mode-payloads.md): a scope line on caching selector results for modes that carry data.
