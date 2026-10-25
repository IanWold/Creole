# How should grammar nodes encode hidden types, and how can engine dispatch over them be checked for completeness?

| | |
| --- | --- |
| Date | 2026-10-18 |
| Cards | [CORE-01](../../active/02-core-combinators/CORE-01-grammar-node-model.md), [FND-07](../../active/01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md), [INT-02](../../complete/03-interpreter/INT-02-plan-lowering-and-cache.md), [RSLN-02](../../active/10-roslyn-analyzers/RSLN-02-share-the-analysis-engine-with-the-roslyn-component.md) |
| Whitepaper | [§ Types](../../../whitepaper.md#types), [§ Where unions are not used](../../../whitepaper.md#where-unions-are-not-used), [§ Custom primitives](../../../whitepaper.md#custom-primitives), [§ Interpreted engine](../../../whitepaper.md#interpreted-engine) |
| Confidence | High on the language and runtime facts: the spec and LDM notes were read, and every claim about compiler behaviour was reproduced on the pinned RC 2 SDK. Medium on the recommendation: Native AOT code size was not measured. |

## Short answer

A `closed` `Parser<TIn, TOut>` cannot hold the nodes Creole needs. C# 15 requires every type parameter of a direct subtype to appear in the closed base, by design and finally [1][2]. That rules out `Map<TIn, TA, TOut>`. Nodes that fix `TOut`, such as `Tok<TIn> : Parser<TIn, TIn>`, compile, but a generic engine cannot cover them without a base-type arm, and that arm switches the check off [1]. `closed` works only if every direct subtype is exactly `X<TIn, TOut>`, with the hidden types one level down. The switch then checks the node *kinds*, and a second call is still needed to recover the types.

Every library examined handles hidden types the same way: the operation that needs them is a virtual method on the node, or a generic visitor method [4][5][6][7]. For Creole, one abstract `internal` method per typed operation (option D below) is the simplest choice that meets both CORE-01 criteria with compile errors rather than warnings. It needs no casts and no generic virtual methods, and it does not depend on `closed`. An `internal` `NodeKind` enum serves the consumers that need only structure.

## Context

CORE-01 planned a `closed` node hierarchy so that "adding a node kind without handling it in an engine's dispatch switch produces a build warning" (§ Where unions are not used). On the first build attempt, the RC 2 compiler rejected `Map<TIn, TA, TOut> : Parser<TIn, TOut>` with CS9383, and the card moved to Designing. This note feeds the decision on node encoding and engine dispatch. The interpreter (INT-02 to INT-05), the analysis adapter (RSLN-02) and every node card (CORE-03 to CORE-12) build on that decision.

## Findings

### The language

1. **The type-parameter rule is deliberate and final.** The spec says: "If a generic class directly derives from a closed class, then all of its type parameters must be used in the base class specification". The reason it gives is "to ensure that there is a single generic instantiation of the derived type that 'exhausts' a given generic instantiation of the closed base type" [1]. The spec's Open questions section reads "N/A" [1]. LDM 2026-04-20 revisited generics and decided: "We will not add a new restriction here. In these generic scenarios, users simply cannot always be exhaustive" [2]. No proposal to relax the rule was found [1][2].
2. **Generic exhaustiveness forces a base-type arm for nodes that fix a type argument.** The spec's example `D2<V> : C<V[]>` gives a "non-exhaustive" warning in `M<X>(C<X> c)` until the switch handles the base type `C<X>` [1]. Constraints are not used to rule subtypes out [1].
   - Measured on RC 2: `Tok<TIn> : Parser<TIn, TIn>` makes a generic switch report CS8509, "'Tok<TOut>' is not covered".
   - Inference: a base-type arm catches every subtype, so a node added later would not warn. For Creole, `closed` gives a useful check only when every direct subtype has exactly the base's type parameters.
3. **Other assemblies cannot derive from a closed type, and an `internal` constructor already gives the same protection.** The spec puts closed subtypes in the "same module" [1]. Measured on RC 2: deriving from a closed type in another assembly is CS9382. Deriving from a non-closed abstract class with only an `internal` constructor is CS1729. So CORE-01's first criterion does not need `closed`.
4. **A reshaped closed hierarchy works, but checks only node kinds.** Measured on RC 2, with a closed root, a non-closed abstract `MapNode<TIn, TOut>` with an `internal` constructor, and `Map<TIn, TA, TOut> : MapNode<TIn, TOut>`:
   - The code compiles.
   - A switch expression missing a kind gives CS8509, naming the kind.
   - Getting at `TA` still needs a virtual or visitor call into `MapNode`.
5. **Only switch expressions are checked.** Measured on RC 2: a switch *statement* that misses a case gets no diagnostic, whether it switches on a closed type or an enum. A switch expression on a nullable input also needs a `null` arm (CS8655).
6. **Enum switch expressions are already checked without closed enums.** Measured on RC 2: a switch expression missing a named member of an ordinary enum gives CS8509. One that covers every named member gives CS8524 for unnamed values, so the project would need to set CS8524 to `none` in `.editorconfig`.
   - Closed enums are not in C# 15. LDM 2026-08-05 places them in the C# 16 focus [3].
7. **`closed` is shipping in C# 15.** A research subagent found it listed under C# 15 in Roslyn's Language Feature Status, and #9499 is labelled "Implemented Needs ECMA Spec" [9]. Not spot-checked.
8. **C# cannot bind a hidden type parameter in a pattern.** Kennedy and Russo proposed a `switch` that binds type parameters (`case Fst<A,B> e:`) and note that implementing it would need reflection [6]. The nearest C# proposal, "type-extracting patterns" (csharplang #3050), was closed and moved to a discussion [15]. It is not in C# 15 (status only, read by a subagent).

### Prior art

9. **Pidgin puts typed rewrites on the node.** Map fusion type-tests against a base that names only the visible types (`parser1 is MapParserBase<TToken, T1> p`). It then calls `internal new abstract MapParserBase<TToken, U> Map<U>(Func<T, U> func)`, which each `MapNParser` overrides with its hidden types in scope [4].
10. **Parlot puts code generation on the node.** Each parser implements `ISourceable.GenerateSource(...)`. A child that cannot be generated throws `NotSupportedException`, which the generator reports as an error at build time. Parlot's docs say "No interceptor namespace or other call-site rewriting configuration is required" [5] (main, 2.0 preview; source read by a subagent, docs spot-checked).
11. **parsley (Scala) uses a visitor with one generic method per node.** Its doc comment says it lets traversals work "without fully explicit exhaustive pattern matching". Sealed types play no part. Its optimiser also uses unchecked casts [7] (read by a subagent).
12. **Kennedy and Russo show both encodings, and where each needs casts.** Their `Fst<A,B> : Exp<A>` is exactly Creole's `Map` shape. A virtual `Eval` handles it with no casts. A visitor `IExpVisitor<T,R>` indexed by the node's result type needs casts for subclasses that fix `T`: "We know that T=int but the compiler does not!" [6].
    - Measured on RC 2: a lowering visitor indexed by `TOut` fails on `Tok<TIn> : Parser<TIn, TIn>` with CS0029 unless it casts.
    - Measured on RC 2: an `internal abstract Plan<TIn, TOut> Lower()` overridden in `Tok`, `Map<TIn, TA, TOut>` and `Many<TIn, T> : Parser<TIn, List<T>>` compiles with no casts, because each override sees the specific type arguments.
13. **.NET's own visitors chose openness.** `ExpressionVisitor` and Roslyn's `CSharpSyntaxVisitor<TResult>` have virtual methods with default bodies, so nothing is exhaustive [8] (read by a subagent).

### The runtime

14. **Generic virtual methods cost Native AOT code size.** The Native AOT overview says: "Generic virtual methods and generic instance methods will also have an instantiation for every implementing or overriding type" [10]. dotnet/runtime#109978 states that "the compiled size in Native AOT with a value type GVM scales at an order of n\*\*2". Its table: 76.3 MB at N=100 against 754.1 KB without the generic virtual method, and "Failed to compile" at N=1000. A runtime engineer adds that "it becomes physically impossible to compile that much code" [11].
    - Inference: a visitor's `Accept<R>` adds a body per node type per instantiation. Creole's lowering runs once per grammar, so call speed is irrelevant and size is the cost. Not measured for Creole.
15. **Calls through generic virtual methods are not devirtualized here.** .NET 11 devirtualizes them when the receiver's exact type is known [12] (read by a subagent). Inference: lowering sees `Parser<TIn, TOut>`, so the call always goes through a run-time lookup.
16. **`(TOut)(object)x` is free for value types.** RyuJIT's importer folds the pattern when the types match: "If so, box/unbox.any is a nop" [13]. For reference types it remains a `castclass`, read by a subagent [14].
17. **Generic cycles are a risk for every option.** IL3054 says Native AOT "cuts off the growth by generating a throwing method". Polly 8.0 hit it with nested tuple state [14] (read by a subagent). Inference: a lowering rewrite that lowers `F(TA)`, such as `List<TA>` or a tuple, through the same generic method could form a cycle, whichever encoding is chosen.

### Who consumes the node tree

18. The cards split into three groups:
    - **Need the hidden types:** interpreter lowering and rewrites (INT-02 to INT-05), tracing (TOOL-02) and the general-parsing spike (GLL-02).
    - **Need only structure (kind, children, metadata):** analysis, which RSLN-02 already requires to run over an abstract grammar model shared with a netstandard2.0 analyzer, plus EBNF export (TOOL-05), Explain (TOOL-03) and fuzzing (TOOL-04).
    - **Never sees runtime node types:** the source generator, which reads syntax and falls back to the interpreter with CRL0301 (§ Source-generated engine).

## The options compared

| | A. `closed`, reshaped | B. Generic visitor | C. `NodeKind` enum only | D. Abstract method per typed operation |
| --- | --- | --- | --- | --- |
| Shape | Closed root; one abstract `XNode<TIn, TOut>` per kind; generic nodes below | `internal abstract R Accept<R>(IVisitor<…, R>)`; one generic method per node | `internal NodeKind Kind`; engines switch on it | e.g. `internal abstract Plan<TIn, TOut> Lower(…)`, overridden per node |
| Hidden types | Needs a second call (B or D) per kind | In scope in the visitor method | Not handled; needs B or D | In scope in the override |
| Missing handler | CS8509 warning, in switch expressions only | CS0535 error | CS8509 warning, in switch expressions only | CS0534 error |
| Casts | Same as its second call | Needed for nodes that fix `TOut` (CS0029) | Needed | None |
| Native AOT | Same as its second call | Generic virtual methods; n² size growth for value types [11] | No cost | Ordinary virtual slots |
| Depends on C# 15 | Yes | No | No | No |
| Precedent | None found | parsley, Kennedy and Russo | `ExpressionType`, `SyntaxKind` | Pidgin, Parlot, Kennedy and Russo |
| Cost | Two mechanisms; breaks silently if a base-type arm or discard is added | One interface per result type; default interface methods would defeat the check | Gives structure, not types | Adding a typed engine adds a member to every node |

Inference on D's cost: few engines need the hidden types (the interpreter and later the GLL spike), so adding an engine is rare. Adding nodes is common, and each new node must implement every operation. The override can stay a one-line call into the engine, so engine logic need not live in node classes: `Map.Lower(ctx) => ctx.Map(source, f)`, where `ctx.Map<TA>` is an ordinary generic method, not a virtual one.

## Disconfirming evidence

- **Against the whitepaper.** § Where unions are not used says a closed hierarchy makes "every engine's dispatch over node types … checked for completeness". Findings 1 and 2 contradict this for nodes with hidden types or fixed type arguments. The § Types sample, edited earlier today to show `closed`, carries the same claim. § Custom primitives repeats it ("a single case of the closed node hierarchy").
- **Against G7 as worded.** No option lets the source generator check its completeness at compile time. It reads syntax and never sees node types (finding 18). Engine agreement for the generator rests on GEN-08's differential tests and on CRL0301 fallback, not on dispatch checks. The whitepaper's "Adding a combinator without teaching every engine becomes a build warning" can at most cover engines inside the `Creole` assembly.
- **Against option D.** Kennedy and Russo, and Oliveira and Cook, note the expression problem. Adding an operation touches every node class [6][16]. parsley chose a visitor for that reason [7].
- **Against option B.** No primary measurement compares a generic-virtual-method visitor with ordinary virtual calls at Creole's scale. The size evidence [11] is for LINQ's `Select`, not a visitor.
- **What was searched.** The csharplang spec, the 2026 LDM notes and roslyn issues, for any relaxation of the type-parameter rule: none found [1][2]. Five libraries and two .NET visitors, for exhaustiveness through sealed or closed types: none found. Runtime docs and issues, for generic virtual method warnings under AOT: none found; they compile without warnings [10].

## Implications for Creole

- **Decision to record:** node encoding and engine dispatch, choosing among A to D. This note supports D plus C:
  - an abstract method per typed operation, which gives completeness as compile errors;
  - an `internal` `NodeKind` with children for consumers that need only structure (the RSLN-02 adapter, EBNF, Explain, fuzzing), checked by enum switch expressions;
  - `Parser<TIn, TOut>` as a plain abstract class with an `internal` constructor, not `closed`.
- **Whitepaper, through the decision:**
  - § Types sample;
  - the "Grammar nodes" bullet of § Where unions are not used;
  - the last sentence of § Custom primitives;
  - possibly § Interpreted engine (lowering through per-node methods);
  - the G7 wording about build warnings.
- **CORE-01:**
  - Scope bullet 1 changes.
  - Criterion 2 becomes stronger: a node that does not implement every engine operation fails to compile.
  - Criterion 1 is met by the `internal` constructor (finding 3).
- **FND-07:**
  - The scope bullet "Confirm `closed` class hierarchies and the completeness warning that engine dispatch relies on (G7)" no longer applies to nodes.
  - The `ParseResult<T>` closed-hierarchy fallback is still possible. Its failure case would have to be `Failed<T>`, because a non-generic `Failed` cannot derive from `ParseResult<T>` (inference from the type rules; not probed).
- **INT-02 and INT-05:** lowering is a per-node method. Map fusion follows Pidgin's pattern [4]: a virtual `Select<TNew>` override on map nodes, which runs once at construction or lowering.
- **INT-05 and the AOT smoke test:** watch for IL3054 when a rewrite lowers a constructed type such as `List<TA>` through the same generic path (finding 17).
- **Generator cards:** a cheap completeness check would be a unit test listing every public combinator. Each must be either emitted by the generator or on an explicit fallback list. This is a suggestion, not settled.

## Open questions

- Native AOT code size of B against D for a realistic node set. A small `tests/Creole.AotSmoke`-style experiment with about 20 node types and value-type results would settle it. Not run, pending the maintainer's agreement.
- Whether the "Tok<TOut>" wording of CS8509 is intended behaviour. It does not change the conclusion.
- Whether `closed` behaves the same on the GA compiler. The spec has no open questions [1], so a change is unlikely. FND-07 confirms on GA.

## Sources

1. dotnet/csharplang. proposals/csharp-15.0/closed-hierarchies.md, main at 05eb480 (2026-08-12). [GitHub](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/closed-hierarchies.md) — relevant sections read (subtyping, type parameter restriction, generics and exhaustiveness, open questions)
2. dotnet/csharplang. LDM notes 2026-04-20, "Closed hierarchies continued". [GitHub](https://github.com/dotnet/csharplang/blob/main/meetings/2026/LDM-2026-04-20.md) — closed hierarchies section, quotes checked
3. dotnet/csharplang. LDM notes 2026-08-05, "C# 16 feature focus: Closed enums". [GitHub](https://github.com/dotnet/csharplang/blob/main/meetings/2026/LDM-2026-08-05.md) — closed enums section, skimmed
4. Pidgin. Pidgin/Parser.Map.Generated.cs at 103dfc6. [GitHub](https://github.com/benjamin-hodgson/Pidgin/blob/103dfc6d6a594310a6681def03bb7445f9a0cefc/Pidgin/Parser.Map.Generated.cs) — `MapParserBase` and fusion read
5. Parlot. main at 143fc0a (2.0 preview): docs/source-generation.md, src/Parlot/Fluent/Then.cs, src/Parlot/SourceGeneration/ISourceable.cs. [GitHub](https://github.com/sebastienros/parlot/blob/143fc0a6c2e1c3c079a2e3ddc2786ec2362e5a00/docs/source-generation.md) — docs spot-checked; source read by a research subagent
6. Kennedy, Russo. Generalized Algebraic Data Types and Object-Oriented Programming. OOPSLA 2005. [PDF](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/gadtoop.pdf) — §1 to §4.3 read by a research subagent; visitor and cast passages checked
7. parsley (Scala). frontend/LazyParsley.scala and frontend/Visitors.scala at de25f0e. [GitHub](https://github.com/j-mie6/parsley/tree/de25f0ecd2d58dd967769e34409b7a03eb7a4996/parsley/shared/src/main/scala/parsley/internal/deepembedding) — read by a research subagent; not spot-checked
8. dotnet/runtime ExpressionVisitor.cs (main, ec0059c) and dotnet/roslyn CSharpSyntaxVisitor.cs (main, 3645b6a). [ExpressionVisitor](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/ExpressionVisitor.cs) — read by a research subagent; not spot-checked
9. dotnet/roslyn. docs/Language Feature Status.md (main, 2026-10-18), and csharplang issue #9499. [Status](https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md), [#9499](https://github.com/dotnet/csharplang/issues/9499) — read by a research subagent; not spot-checked
10. Microsoft Learn. Native AOT deployment overview, limitations. [Learn](https://learn.microsoft.com/dotnet/core/deploying/native-aot/) — paragraph on generics checked in the dotnet/docs source
11. Caruso. Add a feature flag to not use GVM in Linq Select. dotnet/runtime#109978, merged 2025-02-06. [GitHub](https://github.com/dotnet/runtime/pull/109978) — description and the cited comment read
12. hez2010. JIT: Devirtualize non-shared GVMs, dotnet/runtime#122023, and NativeAOT support, #130202 (11.0-preview7). [#122023](https://github.com/dotnet/runtime/pull/122023), [#130202](https://github.com/dotnet/runtime/pull/130202) — read by a research subagent; not spot-checked
13. dotnet/runtime. src/coreclr/jit/importer.cpp, `impBoxPatternMatch` (main, 2026-10-18). [GitHub](https://github.com/dotnet/runtime/blob/main/src/coreclr/jit/importer.cpp) — the cited comment checked
14. Microsoft Learn. IL3054, and App-vNext/Polly#1732. [IL3054](https://learn.microsoft.com/dotnet/core/deploying/native-aot/warnings/il3054), [Polly#1732](https://github.com/App-vNext/Polly/issues/1732) — read by a research subagent; not spot-checked
15. dotnet/csharplang issue #3050, "type-extracting patterns". [GitHub](https://github.com/dotnet/csharplang/issues/3050) — status only, read by a research subagent
16. Oliveira, Cook. Extensibility for the Masses: Practical Extensibility with Object Algebras. ECOOP 2012. [PDF](https://www.cs.utexas.edu/~wcook/Drafts/2012/ecoop2012.pdf) — pages 1 to 3 read by a research subagent
