# ADR-0020: Depth counts each `Rec`, `Bind` continuation and recursive operand, and exceeding it or the stack ends the parse

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-24 |
| Cards | [INT-07](../../complete/03-interpreter/INT-07-recursion-depth-limit.md), [INT-08](../../complete/03-interpreter/INT-08-single-layer-parse-api.md), [BENCH-01](../../active/13-benchmarks/BENCH-01-phase-0-speed-gate.md), [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md), [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md) |
| Whitepaper | [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input), [§ Expressions](../../../whitepaper.md#expressions), [§ Equivalence](../../../whitepaper.md#equivalence) |
| Research | — (prior art read for this record by a research subagent on 2026-10-24, and measurements on the maintainer's machine, cited inline); Parlot's limit from [the Parlot 2.0 note](../research/2026-10-24-parlot-2-0-baselines-and-generator.md) |
| Supersedes | — |

## Context

[§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input) says "`ParseOptions.MaxDepth` bounds recursion depth" and that exceeding it "produces an ordinary diagnostic at the offending span, never a stack overflow; Parlot's recursion limit is the model". [ADR-0004](0004-expression-build-operator-table-and-semantics.md) point 5 adds that "operand recursion, and the right recursion of InfixR, count against `ParseOptions.MaxDepth`". Neither says what one level is, how the parse stops, or what the default is.

Four facts shape the choice:

- **A stack overflow cannot be handled.** "You can't catch a `StackOverflowException` object with a `try`/`catch` block, and the corresponding process is terminated by default" ([`StackOverflowException`](https://learn.microsoft.com/en-us/dotnet/api/system.stackoverflowexception)). The check has to happen before the stack runs out.
- **Where the interpreter recurses.** It recurses without a bound set by the grammar's size in three places only:
  - The cycle-closing edge of a `Rec` cycle. A `Rec` lowers to its target's plan, so today it has no plan step of its own. The `ForwardPlan` on the edge that closes a cycle is the only indirection a cycle costs (`src/Creole/Plans/PlanBuilder.cs`, class remarks).
  - `BindPlan`, whose continuation can return a parser that leads back to the same `Bind` without any `Rec`.
  - `ExpressionPlan`. A prefix operator's operand and an `InfixR` right operand call `ExpressionPlan.Parse` again. Other infix right operands recurse too, but each nested call allows only tighter levels, so the table's size bounds them.

  A custom scanner receives only a cursor (`IScanner.Scan`) and cannot run a parser.
- **The interpreter records no failures yet, and has no public entry point.** INT-08 owns `ParseOptions` and the `Parse` method that returns `ParseResult<T>`, and it waits on ERR-02.
- **Stack per level, measured.** A throwaway console program (not checked in) ran small grammars on the interpreter as built. It read a stack address at the innermost leaf, at depths 100 and 300, and stepped the depth by 10 until `RuntimeHelpers.TryEnsureSufficientExecutionStack()` returned false. The figures are from a Release build on .NET 11 RC 2, Linux x64, i9-13900H. "Cold" is the first parses in a fresh process, before tiered compilation optimized the code.

  | Shape | Input | Bytes per level, cold | Bytes per level, warm | Depth at which the probe turns false, warm (256 KB / 1 MB / 1.5 MB thread) | Unwinding an exception from depth 1,000 / 10,000 |
  | --- | --- | --- | --- | --- | --- |
  | Nested arrays | `[[[1]]]` | 672 | 224 | 560 / 4,070 / 6,410 | 0.25 / 2.5 ms |
  | Nested objects | `{a:{a:1}}` | 368 | 240 | 520 / 3,800 / 5,980 | 0.28 / 2.9 ms |
  | Parentheses in `Expression.Build` | `((1))` | 768 | 332 | 370 / 2,750 / 4,340 | 0.26 / 2.6 ms |
  | Prefix operators | `--1` | 528 | 292 | 420 / 3,120 / 4,910 | 0.19 / 1.9 ms |
  | `InfixR` chain | `1^1^1` | 352 | 400 | 310 / 2,270 / 3,580 | 0.26 / 2.7 ms |

  A full parse of the same deep inputs took 0.015–0.36 ms. Cold code uses up to 3× the stack of warm code, but the ratio is not fixed: the `InfixR` chain came out lower cold than warm. On a 1 MB thread, cold code would reach the probe at an estimated 1,200–2,600 levels, from the cold slopes. `TryEnsureSufficientExecutionStack` cost 1.3 ns a call, against 0.4 ns an iteration for a decrement, compare and increment loop. These grammars have only a few plan steps per level, so real grammars use more.

Prior art, read for this record:

| Source | Counts | Default | When exceeded |
| --- | --- | --- | --- |
| Parlot ([`ParseContext.cs`](https://github.com/sebastienros/parlot/blob/main/src/Parlot/Fluent/ParseContext.cs), [`Deferred.cs`](https://github.com/sebastienros/parlot/blob/main/src/Parlot/Fluent/Deferred.cs), [`Unary.cs`](https://github.com/sebastienros/parlot/blob/main/src/Parlot/Fluent/Unary.cs)) | Each `Deferred` (its `Rec`) entry and each `Unary` operand | `MaxRecursionDepth` 0, "for no limit". Generated parsers take no depth setting | `EnterRecursion` throws `ParseException("The maximum parser recursion depth of {n} was exceeded.", position)` |
| Pidgin, Superpower | Nothing: no match for depth, recursion or `EnsureSufficientExecutionStack` in Pidgin's `Parser.Rec.cs` and `ParseState.cs` or Superpower's `Parse.cs` and `Combinators.cs` | — | Stack overflow |
| Roslyn ([`StackGuard.cs`](https://github.com/dotnet/roslyn/blob/1cca63b5d8ea170f8d8e88e1574aa3ebe354c23b/src/Compilers/Core/Portable/InternalUtilities/StackGuard.cs)) | Its callers' recursion depth | `MaxUncheckedRecursionDepth = 20` | Past 20 levels it calls `RuntimeHelpers.EnsureSufficientExecutionStack()`, which throws `InsufficientExecutionStackException` |
| System.Text.Json ([`JsonReaderOptions.MaxDepth`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonreaderoptions.maxdepth)) and Newtonsoft.Json | JSON nesting | 64 for both | `JsonException`, `JsonReaderException` |

The probe's reserve is fixed by the runtime: "`MinExecutionStackSize = 128 * 1024`" on 64-bit and 64 KB on 32-bit ([`threads.cpp`](https://github.com/dotnet/runtime/blob/a8efd64d46d10c4c0cae224a3304a46aa486c177/src/coreclr/vm/threads.cpp), line 5115). The probe is also supported under Native AOT, according to the research subagent's reading of `RuntimeHelpers.NativeAot.cs`. Default stack sizes are "1.5 MB on Windows and macOS, and 8 MB on Linux", and `System.Threading.DefaultStackSize` changes the size for threads the runtime creates ([threading config](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/threading)). A caller can still create a thread with any size.

The subagent also found disconfirming evidence for a counter on its own. Scriban advisory [GHSA-p6q4-fgr8-vx4p](https://github.com/advisories/GHSA-p6q4-fgr8-vx4p) describes a stack overflow through "a different recursion path not covered by the expression depth counter".

The choice has five forks.

## Options for what counts as a level (fork 1)

### 1A. Every `Rec` entry, every `Bind` continuation, and every recursive operand (chosen)

A level is counted while each of these runs: a `Rec`, whether or not it closes a cycle; the parser a `Bind` continuation returned; a prefix operator's operand; an `InfixR` right operand.

- **Pros.**
  - This is Parlot's rule (`Deferred` and `Unary`), with `Bind` added.
  - The count depends only on the grammar and the input, so the generated engine can count at the same points (G7).
  - It is one sentence in the docs.
- **Cons.**
  - A `Rec` gets a plan step of its own again.
  - Every `Rec` entry pays a counter, including a `Rec` that only defers a forward field reference.
  - A JSON nesting level counts once or twice, depending on how many `Rec` nodes the author put on the cycle.

### 1B. Only a `Rec` that lies on a cycle, plus `Bind` and operands

- **Pros.**
  - A forward-reference `Rec` costs nothing.
- **Cons.**
  - Lowering and the generator both need a cycle analysis.
  - An author must know which `Rec` nodes are on a cycle to predict the count.
  - In the benchmark grammars every `Rec` is on a cycle, so the gate pays the same as with 1A.

### 1C. The plan's cycle-closing edges, plus `Bind` and operands

Count in `ForwardPlan`.

- **Pros.**
  - No new plan step.
- **Cons.**
  - Which edge closes a cycle depends on lowering order and on which root was lowered.
  - The same grammar and input can count differently from different roots, and in the generated engine. That breaks G7.

### 1D. No counter; the stack probe only

- **Pros.**
  - The simplest rule.
- **Cons.**
  - Where a parse fails depends on the thread's stack and on whether the JIT has optimized the code. The table shows a factor of up to 3 between cold and warm.
  - Two runs, or two engines, disagree on inputs a few hundred levels deep.

## Options for how exceeding stops the parse (fork 2)

### 2A. The whole parse ends (chosen)

Exceeding the limit is not a failure that combinators see. The parse unwinds to its entry point, which returns `Failed`. No `Or`, `Optional`, repetition, `Not`, `Lookahead`, `Atomic` or `Recover` inside the parse can catch it. The interpreter uses an internal exception, thrown at most once per parse.

- **Pros.**
  - The grammar's meaning never changes near the limit.
  - It costs nothing on paths that do not exceed it.
  - Parlot also throws.
- **Cons.**
  - Unwinding is slow: 0.25 ms from depth 1,000 (table). This is paid once per hostile parse, and the limit bounds it.
  - An editor cannot `Recover` past a too-deep construct.

### 2B. An ordinary committed failure

- **Cons.**
  - `Not(p)` turns the failure into a success.
  - `Atomic` makes it backtrack, so an `Or` tries its next alternative, which may reach the limit again.
  - What a grammar accepts would depend on the limit.

### 2C. A sticky abort flag on the cursor

- **Pros.**
  - The same outcome as 2A, without an exception.
- **Cons.**
  - Every `Or`, repetition, `Optional`, `Not` and `Lookahead` checks the flag after each failure, on the hot path, for an event that happens at most once.

## Options for the default (fork 3)

### 3A. 1,000 levels (chosen by the maintainer)

- **Pros.**
  - On by default, as the card's summary asks.
  - Far above the nesting of hand-written JSON or code.
  - For the measured grammars, it is below where the probe fires on a 1 MB thread, warm or cold. So the predictable limit, not the stack, decides on the usual thread sizes.
- **Cons.**
  - Legitimate inputs deeper than 1,000 levels fail until the caller raises the limit, for example generated code with a long `else if` chain parsed by recursion.
  - On a 256 KB thread the probe fires first.

### 3B. 256 levels

- **Pros.**
  - Below the probe even on a 256 KB thread, warm.
- **Cons.**
  - Closer to legitimate depths, especially when a nesting level counts twice.

### 3C. No limit; the probe only, as in Parlot

- **Cons.**
  - The cons of 1D become the default behaviour.

## Options for backing the counter with a stack probe (fork 4)

### 4A. No probe, as in Parlot

- **Cons.**
  - The counter cannot promise "never a stack overflow".
  - Stack per level depends on the grammar, the JIT tier and the thread. 1,000 cold levels of the parentheses grammar need about 750 KB, more than a 256 KB thread has.
  - The Scriban advisory shows a counter missing a path.

### 4B. Probe at every counted entry

- **Pros.**
  - The simplest rule that keeps the promise.
- **Cons.**
  - 1.3 ns per entry, measured, on every `Rec`.

### 4C. Probe at every counted entry deeper than 20, as Roslyn's `StackGuard` does (chosen)

- **Pros.**
  - Parses that stay shallow, which includes the benchmark inputs, pay only the counter.
  - 20 unprobed levels take at most about 15 KB in the cold measurements, well inside the runtime's 128 KB reserve.
- **Cons.**
  - One more comparison per entry.
  - It is safe only while 20 levels fit in the reserve, which needs about 6 KB per level.

## Options for the split with INT-08 (fork 5)

### 5A. INT-07 builds the mechanism; INT-08 exposes it (chosen by the maintainer)

- **What it is.**
  - INT-07 builds the counting steps, the counter on the cursor, the probe, and the abort carrying position, depth and cause. It tests them through the internal runner, passing the limit directly.
  - INT-08 adds `ParseOptions.MaxDepth` with its default, catches the abort in `Parse`, and builds the diagnostic.
  - BENCH-01 waits on INT-07, so the Phase 0 gate measures with the checks on.
- **Pros.**
  - INT-07 stays unblocked.
  - The gate is not delayed, because INT-07 is already a wave ahead of INT-08.
- **Cons.**
  - The user-visible half lands with INT-08.

### 5B. INT-07 waits for INT-08

- **Pros.**
  - One card delivers the whole feature.
- **Cons.**
  - INT-07 moves behind ERR-02 and INT-08.
  - BENCH-01 waits one card longer if it also waits on INT-07.

## Decision

1. **What counts.** One level is counted while each of these runs:
   - a `Rec`;
   - the parser a `Bind` continuation returned;
   - a prefix operator's operand in `Expression.Build`;
   - an `InfixR` right operand.

   Nothing else counts, so the depth a parse reaches depends only on the grammar and the input. Each layer's cursor holds its own count, as it holds its own cut flag.
2. **The limit.** `ParseOptions.MaxDepth` is a positive `int` and defaults to 1,000. `int.MaxValue` leaves only the stack probe. Entering a level when the count is already `MaxDepth` ends the parse.
3. **The stack probe.** Each counted entry deeper than 20 also calls `RuntimeHelpers.TryEnsureSufficientExecutionStack()`. If the call returns false, the parse ends the same way.
4. **How the parse ends.** It unwinds past every combinator, `Recover` included, to the parse entry point. That returns `Failed`, with one depth diagnostic added to any already recorded (by `Recover`, ERR-05):
   - **Span:** the item at the position where the level would have been entered, zero-width at the end of input.
   - **Layer:** the layer being parsed.
   - **Unexpected:** a `CustomMessage` that names the cause. That is either `MaxDepth` and its value, so the reader knows which setting to raise, or the thread's stack.
   - **Expected:** empty.
   - **Context:** the context lines in force at that position, once ERR-02 records context.
5. **Cards.**
   - INT-07 builds points 1, 3 and 4 in the interpreter. Its abort carries the position, the depth and the cause.
   - INT-08 adds `MaxDepth` to `ParseOptions` and turns the abort into the diagnostic, so it waits on INT-07.
   - BENCH-01 waits on INT-07, through INT-08.
   - GEN-06 counts and probes at the same points.

Deciding reasons:

- **Fork 1.** 1A is the only rule that is defined on the grammar and is also as simple as the model it follows.
- **Fork 2.** 2A is the only option that neither changes what a grammar accepts (2B) nor taxes every choice (2C).
- **Fork 4.** The measurements and the Scriban advisory show that a counter alone cannot keep the whitepaper's promise. Roslyn's threshold keeps shallow parses free of the probe.
- **Forks 3 and 5.** The maintainer chose these.

What would change this record:

- If BENCH-01 finds the counting step costly, the remedy is in the implementation, not the rule. For example, the `ForwardPlan` on a cycle can do a `Rec`'s count, so a cycle still costs one indirection.
- If a real grammar needs more than about 6 KB of stack per level, the unprobed threshold should drop below 20.

## Consequences

- **Easier.**
  - One rule, from Parlot, that users can learn and that both engines follow.
  - Creole's own recursion never overflows the stack.
  - A depth failure names the setting to raise.
- **Harder.**
  - `Rec` has a run-time cost again: a counter on each entry, and the probe past depth 20.
  - BENCH-01 measures with the checks on.
  - benchmarks/README.md's known differences say that Parlot's limit is off by default and Creole's is on, as [FND-10](../../active/01-foundations/FND-10-move-the-parlot-baselines-to-200.md)'s Outcome asked.
- **Ruled out.**
  - `Recover` cannot continue past a too-deep construct in 1.0.
  - A depth failure never makes a choice try another alternative.
- **Environment-dependent failures.**
  - Below `MaxDepth`, only the probe can end a parse, and where it fires depends on the thread and the JIT tier. This happens when a thread's stack is small or `MaxDepth` is raised.
  - Engines agree on `MaxDepth` failures. Equivalence tests ([§ Equivalence](../../../whitepaper.md#equivalence)) stay below the probe, and do not compare probe failures.
- **Not covered.**
  - Recursion in user code: `Select` and `Where` functions, custom scanners, and code that builds grammars.
  - Lowering itself recurses over the grammar. So a `Bind` continuation that builds a grammar whose nesting grows with the input can still exhaust the stack while that grammar is lowered. This is noted, not solved. Probing in `PlanBuilder` would close it if a real grammar needs that.
- **Watched by.**
  - INT-07's tests, for each counted point:
    - an input at `MaxDepth` levels parses, and one more level ends the parse at the right position;
    - `Not`, `Atomic` and `Or` around the deep part do not change that outcome;
    - an input 100,000 levels deep on a 256 KB thread ends through the probe instead of crashing.
  - GEN-06 runs the same tests on the generated engine.

## Changes made

- Whitepaper:
  - [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input): what counts, the default, the stack probe, and the fact that the parse ends past every combinator. Links this record.
  - [§ Expressions](../../../whitepaper.md#expressions): only a prefix operator's operand and an `InfixR` right operand count.
  - [§ Recovery](../../../whitepaper.md#recovery): `Recover` cannot catch a depth limit.
  - [§ Equivalence](../../../whitepaper.md#equivalence): engines agree on parses `MaxDepth` ends; parses the stack probe ends are not compared.
- Decision index: a note that this record pins [ADR-0004](0004-expression-build-operator-table-and-semantics.md) point 5's "operand recursion".
- Cards:
  - [INT-07](../../complete/03-interpreter/INT-07-recursion-depth-limit.md): scope and criteria restated for the mechanism; it now unblocks INT-08.
  - [INT-08](../../complete/03-interpreter/INT-08-single-layer-parse-api.md): scope adds `ParseOptions.MaxDepth` and the depth diagnostic, with a criterion that a parse exceeding it returns `Failed`; it is now blocked by INT-07.
  - [BENCH-01](../../active/13-benchmarks/BENCH-01-phase-0-speed-gate.md): waits on INT-07 through INT-08, so no direct edge was added. Its scope adds the known-differences note on Parlot's limit.
  - [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md): scope says `Recover` does not catch a depth abort.
  - [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md): scope says it counts and probes at the same points.
