# ADR-0037: Lexers take an initial state through `Layer.Tokens`, parsers set theirs, and reading state of the wrong type throws

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-11-24 |
| Cards | [LAY-04](../../active/06-layers-and-pipelines/LAY-04-layer-local-state.md), [MODE-01](../../active/07-mode-channels/MODE-01-mode-channels-and-layermodal.md), [ANA-05](../../active/09-static-analysis/ANA-05-informational-and-hygiene-checks.md) |
| Whitepaper | [§ 2. Layer-local state](../../../whitepaper.md#2-layer-local-state), [§ Defining layers](../../../whitepaper.md#defining-layers) |
| Research | LAY-04's overload spike, recorded below (2026-11-24, scratchpad, not checked in) |
| Supersedes | — |

## Context

§ 2. Layer-local state defines `GetState<S>()`, `SetState<S>(s)` and `p.Scan<S, R>((value, state, span) => (result, newState))`. Marks snapshot the state by reference. [ADR-0030](0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) gives every layer its own state, says what each mechanism restores, and lets marks leave state out of a grammar with no `SetState`, `Scan` or `Bind`. [ADR-0026](0026-engines-may-skip-unused-values-and-repeat-user-functions.md) makes `Scan`'s function a decision function. Three questions remain open for LAY-04.

1. **Where a lexer's state starts.** Only `Layer.Transduce` takes an `initialState`. § Feedback between layers lists template-string brace depth and heredoc terminators as uses of layer state, and § 3. Mode channels puts a heredoc's terminator in "the lexer's own layer state". LAY-04's criterion is a template-string brace-depth lexer. `Layer.Tokens` has no state parameter.
2. **Whether a parser has state.** A parser here is a single-layer `Parser.Parse`, or the parser at the end of a pipeline. ADR-0030 lists layer-local state among the parts each layer holds, and INC-04's memos store a parser rule's entry state. Nothing says where a parser's state starts.
3. **Missing or mistyped state.** `GetState<S>` names its type at each use. A layer's state has one type, or none. Nothing says what `GetState<int>()` does in a layer whose state is an `ImmutableStack<int>`, in a layer with no state, or what `atEnd` receives after a step sets a state of another type.

### The spike

LAY-04 checked overload resolution on SDK 11.0.100, `LangVersion` 15, with stand-in types shaped like `Layer.Tokens`'s current overloads plus stateful ones whose first parameter is `TState initialState`. Each stateless call was compiled with the stateful overloads present.

| Overloads added | Call | Result |
| --- | --- | --- |
| `(initialState, item)` and `(initialState, item, skip)` | `Layer.Tokens(item, skip)` | CS0121: ambiguous with `(initialState, item)`, inferring `TState` as the item's parser type and `TOut` as `Unit` |
| The same | `Layer.Tokens(item, skip, "words")` | CS0121, as above |
| `(initialState, item, skip)` only | `Layer.Tokens(item)`, `(item, skip)`, `(item, name: "words")`, `(item, skip, "words")` | Each binds to its stateless overload |
| The same | `Layer.Tokens(0, item, skip)`, `Layer.Tokens(ImmutableStack<int>.Empty, item, skip, name: "x")`, and named arguments | Each binds to the stateful overload |

## Options

### Fork 1: how a lexer gets its state

#### 1A. A `Layer.Tokens` overload that takes `initialState`, `item` and `skip` (recommended)

`Layer.Tokens(initialState, item, skip, name)`, with `initialState` first as in `Transduce`.

- Pros: a lexer threads state through its item parser, as § 2 says. `skip`, token spans and errors work as they do today. It follows `Transduce`'s parameter order.
- Cons: two-argument and three-argument stateful forms cannot both exist without breaking `Layer.Tokens(item, skip)` (spike), so a stateful lexer always passes `skip`. One with nothing to skip passes `Return(new Unit())`.

#### 1B. Stateful lexers use `Transduce`

No new API. A step over skipped input emits `[]`, and a step over a token emits it.

- Pros: nothing to add. `Transduce` already takes and threads a state.
- Cons: every lexer with state wraps each token in an `Emit` and turns skipping into steps that emit nothing. The default layer name is `layout`. The whitepaper's examples treat the lexer as the layer that holds such state, so this would need a whitepaper change to say lexers with state are written another way.

#### 1C. A separately named method or a `WithState` on the layer

For example, `Layer.Tokens(item, skip).WithState(0)`.

- Pros: no overload interplay at all.
- Cons: `WithState` means nothing on `Where`, `Select`, `Identity` or `Utf8` layers, so it either throws there or needs a public layer subtype. A second name for the same layer adds a concept.

### Fork 2: where a parser's state starts

#### 2A. The grammar sets it (recommended)

A parser starts with no state. Its grammar sets one before reading it: `SetState(0).Then(Root)`.

- Pros: no API change. A parser runs once per parse, or once per item under `ParseMany`, so a `SetState` at its root runs once there, and the starting value is data in the grammar.
- Cons: reading before setting needs Fork 3's rule. The pattern does not carry over to a layer that runs in steps, where a `SetState` at the start of the item would run on every step.

#### 2B. A `Parse` or `Pipeline.Then` parameter

`parser.Parse(text, initialState)`, and later `Pipeline.Then(parser, initialState)`.

- Pros: the parser's type of state is declared where it starts, as for layers.
- Cons: more overloads across INT-08's and INT-11's `Parse` and LAY-05's pipelines, so it would wait for LAY-05. It adds nothing a `SetState` at the root cannot do.

#### 2C. No parser state in LAY-04

`GetState`, `SetState` and `Scan` work only in layers that run in steps.

- Pros: the least to build.
- Cons: it contradicts ADR-0030, which gives every layer state, and INC-04, which memoizes a parser rule's entry state. A single-layer parser over characters could not use state at all.

### Fork 3: reading state that is missing or of another type

#### 3A. Throw at run time (recommended)

`GetState<S>` and `Scan<S, R>` throw `GrammarException` when there is no state or it is not an `S`. A layer given an `initialState` of type `TState` throws when a step leaves a state that is not a `TState`.

- Pros: a mistake never gives a silent wrong result. It is one type test per read, and one per step in a layer with a declared type. It covers `Bind` continuations, which no earlier check can see.
- Cons: a mistake shows only when a parse reaches it, and only on inputs that reach it.

#### 3B. Also check when the layer opens

As 3A, plus a walk of the layer's grammar, following `Rec`, that compares each `GetState`, `SetState` and `Scan` type with the layer's `TState` before any input is read.

- Pros: the common mistakes fail on the first parse, whatever the input.
- Cons: plans are cached per parser, not per layer, and one parser can serve layers with different state types, so the walk runs per layer and is not cached with the plan. Parsers have no declared type, so they still rely on 3A. A build-time check belongs to analysis, which reports through CRL diagnostics.

#### 3C. Read missing or mistyped state as `default(S)`

- Pros: never throws. An `int` depth starts at 0 with no `initialState`.
- Cons: `GetState<long>()` in a layer whose state is an `int` always reads 0, and the parse gives wrong results with no sign of why. `default` of an `ImmutableStack<T>` or a record is null, so the common state types still need a starting value.

## Decision

Options **1A, 2A and 3A**. The maintainer chose each fork and accepted the record on 2026-11-24.

1. **Which layers have state.** Every layer that runs a grammar has one state: `Layer.Tokens`, `Layer.Transduce`, and `Layer.Modal` once MODE-01 builds it. So does every parser. `Where`, `Select`, `Identity` and `Utf8` run no grammar and have none. A state is any value, held by reference, so a value-type state is boxed each time it changes.
2. **Lexers.** `Layer.Tokens<TIn, TOut, TState>(TState initialState, Parser<TIn, TOut> item, Parser<TIn, Unit> skip, string name = "lexer")` makes a lexer whose state starts as `initialState`. `item` and `skip` share the state. There is no overload of `initialState` and `item` alone, because it makes `Layer.Tokens(item, skip)` ambiguous (the spike). A lexer with nothing to skip passes `Return(new Unit())`. Each slot records the state before its step as `stateBefore`. MODE-01 gives `Layer.Modal` the same form, with `initialState` first, and checks its overloads for CS0121 as the spike did.
3. **Where state starts.** A layer given an `initialState` starts with it. Every other layer that runs a grammar, and every parser, starts with no state. `SetState` and `Scan` give it one, which then lasts as any state does: across steps, restored by marks. A parser sets its state at its root: `SetState(0).Then(Root)`. A layer that runs in steps takes an `initialState` instead, since a `SetState` at the start of its item runs on every step.
4. **Reading.** `GetState<S>()` succeeds without consuming and returns the state when it is an `S`. `Scan<S, R>` reads the state the same way before it calls its function. Null counts as an `S` when `S` admits null. When there is no state, or it is not an `S`, both throw `GrammarException`, naming the layer, `S`, and the state's type or that there is none. As with a step that consumes nothing, the parse throws rather than returning an error, because the grammar is wrong, not the input.
5. **Writing.** `SetState<S>(s)` and `Scan` store any value. A layer given an `initialState` of type `TState` checks at the end of each step that its state is a `TState`, with null counting when `TState` admits null, and throws `GrammarException` otherwise. Later steps and `atEnd` read it as a `TState`, and the check names the step that broke it rather than a later read. A parser declares no type, so only its reads check.
6. **No build-time check yet.** Nothing checks state types when a layer is built or opened. ANA-05 gains a check that each `GetState`, `SetState` and `Scan` in a layer's grammar uses the layer's state type. Its CRL number is assigned when it is built.

Deciding reasons:

- **1A:** it is the only option that gives lexers state as the whitepaper describes them while keeping `skip`, spans and existing calls as they are. The spike rules out its two-argument form.
- **2A:** a parser runs once, so a `SetState` at its root is its starting state, and no API is needed.
- **3A:** it is the simplest rule that never gives a wrong result silently, and it is the only check that also covers `Bind` continuations.

What would change this:

- Users hit point 4's exception often in practice. ANA-05's check would then come earlier, or 3B's walk would be added.
- A measured cost from boxing a value-type state in a real lexer, such as LAY-07's. That would argue for a typed state slot.
- A caller that must supply a parser's starting state, such as an incremental session (INC-04). That would argue for 2B.

## Consequences

- **Easier.** A lexer with state is an ordinary `Layer.Tokens`. Parsers need no new API. The three uses in § Feedback between layers, brace depth, indentation and heredoc terminators, each have a layer to hold their state.
- **Harder.** A lexer with state always passes `skip`. A state mistake shows only when a parse reaches it, and the parse throws rather than returning an error. A value-type state, such as an `int` brace depth, allocates a box each time it changes.
- **Unchanged.** Grammars with no `SetState`, `Scan` or `Bind` take the marks they take now (ADR-0030 point 9), so Phase 0 parsers do not save state.
- **Second runs.** Under [ADR-0027](0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md), the run with a failure log starts every layer and parser from its starting state again, so `Scan`'s function sees the same states and the run takes the same path.
- **Ruled out.** Reading missing or mistyped state as `default(S)`, and a `Layer.Tokens(initialState, item)` overload.
- **Watch.** The state adds a field to each layer's parse state. LAY-04's build compares benchmark runs before and after with `scripts/bench-compare.py`.

## Changes made

- Whitepaper:
  - [§ 2. Layer-local state](../../../whitepaper.md#2-layer-local-state): a new paragraph says that every layer that runs a grammar has one state, and so does every parser. It says where state starts: an `initialState` for `Transduce` and `Layer.Tokens(initialState, item, skip)`, otherwise none, and a parser's own `SetState` at its root. It says that reading missing or mistyped state, or a step that leaves a state of another type, throws `GrammarException`. Links this record.
  - [§ Defining layers](../../../whitepaper.md#defining-layers): the paragraph after the samples says a lexer with state passes it first, `Layer.Tokens(initialState, item, skip)`, and always passes `skip`, and why.
  - Checked and unchanged: § Functions in grammars (`Scan`'s function is a decision function), § Bookmarks and retention, § Properties and § Layer fusion, which name layer state but say nothing this record changes.
- Cards:
  - [LAY-04](../../active/06-layers-and-pipelines/LAY-04-layer-local-state.md): scope follows points 1 to 5, plus the benchmark comparison under § Consequences. A second criterion covers a parser that sets its state with `SetState`, and the `GrammarException` cases of points 4 and 5. Moved to Building.
  - [MODE-01](../../active/07-mode-channels/MODE-01-mode-channels-and-layermodal.md): a `Layer.Modal` form that takes an `initialState` first, with its overloads checked for CS0121 (point 2).
  - [ANA-05](../../active/09-static-analysis/ANA-05-informational-and-hygiene-checks.md): the state-type check of point 6 in scope, a criterion that it fires on `GetState<long>()` in a layer whose state is an `int`, and a link to § 2. Layer-local state.
