# ADR-0043: `Layer.Modal` plans each distinct mode value, data included, keeps the 64 most recently used, and needs no `ModeData<T>()`

| | |
| --- | --- |
| Status | Proposed |
| Date | 2026-11-26 |
| Cards | [MODE-05](../../active/07-mode-channels/MODE-05-mode-payloads.md), [ANA-05](../../active/09-static-analysis/ANA-05-informational-and-hygiene-checks.md), [FUS-03](../../active/16-layer-fusion/FUS-03-fusion-with-modes-and-custom-scanners.md) |
| Whitepaper | [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels), [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions), [§ Conditions](../../../whitepaper.md#conditions) |
| Research | Two spikes on SDK 11.0.100, recorded below (2026-11-26, scratchpad, not checked in) |
| Supersedes | — (refines [ADR-0041](0041-a-channel-has-one-reader-below-one-writer-and-a-write-nothing-reads-or-a-pop-of-the-last-mode-throws.md) point 6) |

## Context

§ 3. Mode channels, under **Mode payloads**, says `TMode` can be a union and its cases should carry no data. Data the lexer can decide by itself, such as a heredoc's terminator, belongs in layer state. For the rare case where only the parser knows the data, "the item parser reads them with `ModeData<T>()`, and that layer gives up fusion".

[ADR-0041](0041-a-channel-has-one-reader-below-one-writer-and-a-write-nothing-reads-or-a-pop-of-the-last-mode-throws.md) point 6 says the selector "runs at most once per distinct mode for the life of the layer". Its consequences say the same thing for this card: "A mode case that carries data gets one entry per value, so payload modes need a bound or a cache keyed by case." MODE-01 built the cache as two lists: one on the layer, searched under a lock, and one per run, searched without it ([ModalLayer.cs](../../../src/Creole/Layers/ModalLayer.cs)). Each step searches the run's list with `EqualityComparer<TMode>.Default`.

The two texts pull apart. `ModeData<T>()` is needed only if the selector does not see each value, which means caching per case. But a selector called once per case still receives a value, and nothing stops it from using that value. The natural arm `Heredoc h => String(h.Terminator)` would then keep the first terminator for every later heredoc, silently.

### The spikes

Both ran on SDK 11.0.100 with `LangVersion` 15. The modes were `union LexMode(Code, Heredoc)`, with `sealed record Code` and `sealed record Heredoc(string Terminator)`.

| Question | Finding |
| --- | --- |
| Does a union compare by case value? | Yes. `EqualityComparer<LexMode>.Default` is `ObjectEqualityComparer`, through `ValueType.Equals`. Two `Heredoc("EOF")` are equal with equal hashes, `Heredoc("EOF")` and `Heredoc("END")` differ, and a `Dictionary<LexMode, int>` finds a key by an equal value. A union of a `record struct` case compares the same way. The union struct implements `IUnion`, not `IEquatable<LexMode>`. |
| What does a comparison cost? | 80 B and 35–55 ns for a union, against 0 B and under 1 ns for an enum. Comparing the case types (`Value?.GetType()`) costs about 3 ns and allocates nothing. These are single Stopwatch runs of 10 million comparisons, Release build, not BenchmarkDotNet. |
| Is an exhaustive switch over the union accepted? | Yes. `m switch { Code => …, Heredoc h => … }` compiles with no `_` arm. |

Reading the code adds one fact. `Bind` lowers the parser its continuation returns through `PlanCache`, which is keyed by node identity, so "a parser the continuation builds afresh at each match is lowered at each match" ([BindPlan.cs](../../../src/Creole/Plans/BindPlan.cs)). An item parser written `ModeData<string>().Bind(t => String(t))` would therefore lower a new plan on every step.

### Where payloads arise

No grammar on the roadmap needs a payload. Example C's modes carry no data, and a heredoc's terminator belongs in layer state. One kind of language does need parser-only data: one whose declarations add tokens, as Coq's `Notation` commands are said to (*unverified*). There the lexer's item parser is built from data that only the parser has, and it changes rarely.

### What is fixed already

- The maintainer chose per-value caching with a bound as the direction on 2026-11-26, over data beside the mode, deferring payloads, and caching per case.
- A channel has one reader and one writing layer, and its stack belongs to the writer (ADR-0041 points 2 and 3). Nothing here changes that.
- Mode changes, their log, invalidation and rollback belong to MODE-02 and MODE-03, and nothing here changes them. MODE-02's Proposed ADR-0042 (on its own branch) treats a write as a change only when the new top mode differs from the old by `EqualityComparer<TMode>.Default`. It relies on the same equality as this record's cache. So a payload needs value equality there too, and for a union mode each write's comparison boxes, as the spike measured.

## Criteria

1. **Correct by construction.** A selector or item parser written in the natural way lexes each payload with its own data. Test: one parse that writes two payloads of the same case with different data, each lexed with its own.
2. **Bounded memory.** A layer's cached plans never exceed a fixed count, however many distinct mode values it sees in its life. Test: 10,000 distinct payloads through one layer, with the parse correct and the cache at or under the bound.
3. **Steps cost what they did.** A step whose mode has not changed lowers no plan, calls neither the selector nor `Equals`, and allocates nothing. Measure: a new `LayerBenchmarks` case, `ModalUnion`, runs the JSON lexer behind a `Layer.Modal` over a one-case union with no data. It is compared with the existing `Modal` (one-mode enum) case on each JSON input, using the median of three runs per input. It must allocate no more per parse than `Modal` plus a constant that does not grow with the input, and take at most 1.05 times as long.
4. **Modes without data keep ADR-0041 point 6.** An enum or a union without data, with at most the bound's number of modes, calls its selector at most once per mode for the life of the layer. `EachModesItemParserIsPlannedOnce` stays green.
5. **API.** No public member is added that the chosen option does not need, and no call can bind to an overload by mistake. Where an option adds overloads, a spike checks CS0121, and checks that a `string` argument cannot fall into a `[CallerArgumentExpression]` parameter.
6. **Analysis.** There is one identifiable place where analysis learns that an item parser depends on data. ANA-05 can report CRL0201 there, and FUS-03 can test for it.

## Options

### Fork A: how data reaches the item parser

#### A1. Per value, unbounded

Keep ADR-0041 point 6 as built. The selector sees each mode value, data included, and is called once per distinct value. A dictionary replaces the lists. `ModeData<T>()` is not needed, because the selector already has the data.

- Pros: correct by construction. Nothing to build but the dictionary.
- Cons: the layer is usually a `static` field that lives as long as the process. Each distinct payload adds a plan that is never freed, so a server that parses many documents grows without limit.

**Result:** Ruled out by argument: it fails criterion 2.

#### A2. Per value, bounded (recommended)

As A1, but the layer keeps at most a fixed number of plans (Fork B). When a mode's plan is no longer cached, the next step that needs it calls the selector again.

- Pros: correct by construction. The selector may use the data, so `Heredoc h => String(h.Terminator)` is right. An item parser built from the data, such as a `Longest` over a token table, is lowered once per value, not once per step. No public member is added, and the whitepaper's planned `ModeData<T>()` is dropped. Enums and unions without data behave as under ADR-0041 point 6.
- Cons: a payload value that leaves the cache and comes back is planned again. A payload must have value equality and must not change after it is written (point 6 of the Decision). The whole layer is opaque to analysis and gives up fusion, which the whitepaper already says of a payload layer.

**Result:** Open. Recommended. Criteria 1, 2 and 4 are settled by tests in the build, and criterion 3 by trial T1.

#### A3. Per case, with `ModeData<T>()`, as the whitepaper reads

Cache per union case type. The selector runs once per case with the first value it sees. The item parser reads the current mode's data with `ModeData<T>()`.

- Pros: the cache is bounded by the number of cases. It needs no change to the whitepaper.
- Cons: a selector arm that uses its case variable is silently wrong after the first value, and nothing can detect it. An item parser whose structure depends on the data goes through `Bind` and lowers a plan on every step. "Case" means nothing for a mode type that is not a union.

**Result:** Ruled out by argument: it fails criterion 1, and criterion 3 for data that shapes the item parser.

#### A4. Data beside the mode, with `ModeData<T>()`

Modes stay plain: enums, or unions without data. A write carries the data separately, as in `PushMode(Modes, LexMode.Raw, name)`, and the stack keeps it next to the mode. The item parser reads it with `ModeData<T>()`, which throws `GrammarException` when there is no data or it is not a `T`, as `GetState` does (ADR-0037 point 4). The cache stays per mode.

- Pros: the selector never sees the data, so it cannot capture it. Modes can still be listed, so analysis and FUS-03 lose only the modes whose item parser reads data.
- Cons: new overloads of `SetMode` and `PushMode`. The present `SetMode<TMode>(channel, mode, string channelExpression = "")` would take a call such as `SetMode(Modes, LexMode.Raw, "EOF")`, binding the data to the caller-expression parameter. That would need a spike, or a different name. An item parser whose structure depends on the data goes through `Bind` and lowers a plan on every step, unless the user caches parsers by data.

**Result:** Ruled out by argument: it fails criterion 3 for data that shapes the item parser, and adds overloads that criterion 5 would have to clear. The maintainer preferred A2.

#### A5. Defer payloads

Build union modes without data only. Document that data belongs in layer state, remove `ModeData<T>()` from the whitepaper, and cut MODE-05's scope. A mode type with data keeps A1's behaviour, as an unsupported case.

- Pros: the least to build. No known grammar needs a payload.
- Cons: the unsupported case still leaks, and fails criterion 2. The card's purpose goes.

**Result:** Ruled out by argument: it fails criterion 2. The maintainer chose to support payloads.

### Fork B: the bound

#### B1. Clear all at 64

When a new mode would make a 65th plan, the layer drops every plan and starts again.

- Pros: the simplest. It keeps no order.
- Cons: a stream of distinct payloads drops the modes without data every 64 payloads. A selector that builds its nodes inline, as Example C's does, makes `PlanCache` lower them again, and a large `Longest` builds its automaton again.

**Result:** Open. The simpler alternative to B2.

#### B2. Evict the least recently used beyond 64 (recommended)

The layer keeps plans in order of use. A new mode beyond 64 evicts the plan used longest ago.

- Pros: modes in steady use, such as a `Code` mode between heredocs, stay planned while payloads churn. Order is updated only when a step's mode differs from the last step's (Fork C), so the extra work comes only with a mode change.
- Cons: a little more code than B1, under the layer's lock.

**Result:** Open. Recommended, by argument: only payloads churn, and the modes they would push out are the ones in steady use.

#### B3. Throw past a bound

More than a set number of distinct modes throws `GrammarException`.

- Pros: memory is bounded and nothing is planned twice.
- Cons: payloads become unusable whenever their values are unbounded, which is the case they exist for.

**Result:** Ruled out by argument.

**The number.** 64 is far above any lexer we know of: Example C has two modes, and no grammar on the roadmap has more than a handful. A grammar that uses more than 64 modes without data would plan some of them again. That is a cost, not a wrong result.

### Fork C: finding the plan for a step

#### C1. Compare on every step, as MODE-01 built

Each step searches the run's list of used modes with `EqualityComparer<TMode>.Default`.

- Pros: already built.
- Cons: for a union mode, each comparison allocates 80 B (the spike), on every step, for every mode searched past. The run's list also grows with each distinct payload.

**Result:** Ruled out by trial T1. Three default-job runs of `LayerBenchmarks` on 12e48bb, with the `ModalUnion` case added and not yet committed, gave these results. `ModalUnion` took 1.437, 1.447 and 1.461 times as long as `Modal` on the small, medium and large inputs (median of three runs per input). It allocated 21,712 B, 1,522,658 B and 15,129,967 B per parse, against `Modal`'s 3,072 B, 145,216 B and 1,434,664 B. The difference grows with the input: about 80 B per token, as the spike measured. That fails criterion 3. `Modal` took 1.006 to 1.012 times as long as `Tokens`.

#### C2. Reuse the plan while the stack is unchanged (recommended)

Each run remembers the last stack it read and that stack's plan. A step whose stack is the same object uses the plan without comparing. Only a changed stack is looked up by value. `SetMode` already reuses one stack per plan when nothing is pushed ([SetModePlan.cs](../../../src/Creole/Plans/SetModePlan.cs)), so a grammar that switches between the same modes compares only on each switch. Whether a run also keeps a small list of its own, to keep lookups off the layer's lock, is an implementation choice that criterion 3 and MODE-01's parallel test judge.

- Pros: steps between mode changes cost one reference comparison, for any mode type.
- Cons: a mode change still compares by value. For a union, that allocates.

**Result:** Open. Recommended. Trial T1 settles it.

#### C3. Compare unions without boxing

Read the union's case value directly, through the struct's layout, or through a comparer built by reflection for `IUnion` types.

- Pros: comparison would cost what comparing case values costs.
- Cons: the layout of a union struct is the compiler's, not a contract. A comparer built by reflection over a value type is not safe under Native AOT (G8).

**Result:** Ruled out by argument.

### Fork D: what analysis treats as opaque

#### D1. The layer (recommended)

With `ModeData<T>()` gone, no node marks the dependence on data: the selector is the function from a value to a parser. A `Layer.Modal` is opaque for each mode analysis cannot list, and ANA-05 reports CRL0201 there, naming the layer. FUS-03 does not fuse such a layer, and CRL0302 names the reason. How analysis lists a channel's modes is ANA-01's question. It could take every value of an enum, or the channel's initial mode plus every mode written as a constant in the grammar above. MODE-05 builds no analysis.

- Pros: one place, the layer, as criterion 6 asks. It matches the whitepaper's "that layer gives up fusion".
- Cons: until analysis can list a union's cases, a union mode without data may be reported as opaque too. That would be a false positive under the Phase 2 gate, so ANA-01 must list modes before ANA-05 reports.

**Result:** Open. Recommended.

#### D2. A node marks it

`ModeData<T>()` would mark the dependence, as `Satisfy` marks an opaque predicate.

**Result:** Ruled out by Fork A, which drops `ModeData<T>()`.

### Fork E: writing a mode computed from the input

Under A2, a payload comes from the parse, such as a name the parser has just read.

#### E1. Through `Bind` (recommended)

`Ident.Bind(name => PushMode(Modes, (LexMode)new Raw(name)))`. The mode node is built and lowered at each match.

- Pros: no new member. Writes of payloads are rare, so lowering at each one costs little.
- Cons: the `Bind` hides the write from analysis. ADR-0030 already treats a grammar with a `Bind` as one that can change the mode, so its marks stay correct.

**Result:** Open. Recommended, by argument.

#### E2. Value-taking writes

`Ident.PushMode(Modes, name => new Raw(name))`, a node that runs its parser and pushes the mode computed from its value.

- Pros: analysis sees the channel written. Nothing is lowered at each match.
- Cons: new members, before any grammar is known to need them. They can be added later without breaking a call.

**Result:** Open. Not recommended now.

## Trials

| Trial | Settles | How |
| --- | --- | --- |
| T1 | Fork C, criterion 3 | Add `ModalUnion` to `LayerBenchmarks`. Run `--filter '*LayerBenchmarks*'` three times with C1 as built, then three times with C2. Compare medians per input against `Modal`. |
| Tests | Criteria 1, 2 and 4, and Fork B | Two payloads of one case lexed with their own data. 10,000 distinct payloads with the cache at or under 64 and the parse correct. A mode without data used between churning payloads planned once (B2). `EachModesItemParserIsPlannedOnce` green. |

No other option needs a trial. Each is settled by the argument in its Result line.

## Decision

Recommended: **A2, B2, C2, D1 and E1**. Each is open until the build's tests and trial T1 settle it.

1. **Modes may carry data.** `TMode` can be any type with value equality, including a union whose cases carry data. The selector receives the mode value, data included, and may use it. Modes compare with `EqualityComparer<TMode>.Default`, as ADR-0041 point 1 says.
2. **The cache.** A `Layer.Modal` keeps the plans of at most 64 distinct modes. Past 64, it evicts the plan used longest ago. The selector runs at most once per distinct mode while that mode's plan stays cached. For a layer with at most 64 modes, it still runs at most once per mode for the life of the layer, so ADR-0041 point 6 holds for every grammar it was written for. The rest of point 6 stands: concurrency, null results and exceptions.
3. **Steps.** A step whose channel stack is the same object as the last step's reuses that step's plan without comparing modes.
4. **No `ModeData<T>()`.** The selector is how an item parser reads a payload.
5. **Writes.** A mode computed from the input is written through `Bind`.
6. **Payload values.** A payload should be immutable and have value equality, as layer state should. A payload changed after it is written can match a plan made for its old value. Records and unions of records qualify.
7. **Analysis and fusion.** A `Layer.Modal` is opaque to analysis for each mode analysis cannot list. ANA-05 reports CRL0201 at the layer, and FUS-03 does not fuse it. ANA-01 decides how modes are listed.
8. **Documentation.** The mode channels page says that data the lexer can decide belongs in layer state, with a heredoc example. It also covers payloads, for data only the parser has, and the cache's bound.

What would change this:

- A grammar whose item parser uses its data only in actions, where planning once per value is measurably slow. That would argue for adding A4's `ModeData<T>()` beside A2. It can be added without breaking a call.
- A real grammar that uses more than 64 modes without data, or a payload stream that evicts modes in steady use. Either would argue for a larger bound or a configurable one.
- T1 showing that C2 misses criterion 3 for unions. That would reopen C3, or a rule that modes be enums.

## Consequences

- **Easier.** A payload needs no new API. A selector arm that uses its data is right. Union modes without data cost nothing per step beyond an enum, once C2 lands.
- **Harder.** A payload that churns plans the item parser again for each value it brings back. A mutable payload is a mistake that nothing catches yet. ANA-05's CRL0204 could cover mode types as it covers layer state.
- **Ruled out for now.** `ModeData<T>()`, data beside the mode, value-taking writes and a configurable bound. Each can be added later without breaking a grammar.
- **For ANA-01 and ANA-05.** Analysis must list a channel's modes before it reports CRL0201 at a `Layer.Modal`, or union modes without data become false positives.
- **For FUS-03.** "Only a finite mode channel" in § Conditions means a channel whose modes analysis can list. A layer with payloads is not fused.
- **Watch.** T1's `ModalUnion` case stays in `LayerBenchmarks`, so a change to the step loop shows up for union modes as well as enums.

## Changes made

None yet. On acceptance:

- Whitepaper: § 3. Mode channels, in its selector paragraph ("once per distinct mode case … for the life of the layer") and its **Mode payloads** paragraph, which drops `ModeData<T>()`. § Using C# 15 unions keeps its lexer-modes row, with a check that it still reads right. § Conditions, if its "finite mode channel" wording needs the meaning given above.
- Cards: MODE-05's scope and criterion. ANA-05 gains a scope line and criterion for CRL0201 at a `Layer.Modal`. FUS-03 gains a scope line saying a layer with payloads is not fused. ANA-01 gains a scope line for listing a channel's modes.
- Index: a note that this record refines ADR-0041 point 6.
