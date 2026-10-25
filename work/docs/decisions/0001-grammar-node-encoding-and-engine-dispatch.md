# ADR-0001: Grammar nodes use an internal constructor and one abstract method per engine operation, not a `closed` hierarchy

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-18 |
| Cards | [CORE-01](../../active/02-core-combinators/CORE-01-grammar-node-model.md), [FND-07](../../active/01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md), [INT-02](../../complete/03-interpreter/INT-02-plan-lowering-and-cache.md), [RSLN-02](../../active/10-roslyn-analyzers/RSLN-02-share-the-analysis-engine-with-the-roslyn-component.md) |
| Whitepaper | [§ Types](../../../whitepaper.md#types), [§ Where unions are not used](../../../whitepaper.md#where-unions-are-not-used), [§ Custom primitives](../../../whitepaper.md#custom-primitives), [§ Interpreted engine](../../../whitepaper.md#interpreted-engine) |
| Research | [Grammar node encoding and dispatch](../research/2026-10-18-grammar-node-encoding-and-dispatch.md) |
| Supersedes | — |

## Context

The whitepaper made `Parser<TIn, TOut>` a C# 15 `closed` class. The aim was that a switch over node types in each engine would be checked for completeness, and a combinator added without engine support would produce a build warning.

The C# 15 rules make that impossible for Creole's nodes. The research note has the details:

- **Hidden type parameters are rejected.** Every type parameter of a type that derives directly from a closed class must appear in the base (CS9383). That rules out `Map<TIn, TA, TOut> : Parser<TIn, TOut>`, and with it `Select`, `Then`, `Before` and `Bind`. The rule is final.
- **Fixed type arguments defeat the check.** Nodes such as `Tok<TIn> : Parser<TIn, TIn>` or `Many<TIn, T> : Parser<TIn, List<T>>` compile. But a generic engine can only cover them with an arm for the base type, and that arm also hides any node added later.
- **C# has no pattern that binds a hidden type parameter**, so an engine outside the node cannot recover `TA` without a second dispatch.

The forces at play:

- **Completeness:** engine support should be checked when a node kind is added. This protects G7.
- **Value types:** results must stay unboxed, per § Performance design.
- **Native AOT:** grammars must stay safe and small under Native AOT (G8).
- **Simplicity:** the maintainer prefers the simplest approach that a check can protect.

## Options

### A. Keep `closed`, reshaped

A closed root, one non-closed abstract `XNode<TIn, TOut>` per node kind, and generic nodes below those. Engines switch over kinds, then make a second call into the kind to reach the hidden types.

- Pros: closest to the whitepaper's position. Another assembly that derives from the root gets CS9382.
- Cons:
  - Two dispatch mechanisms.
  - Only switch expressions are checked, and only with a warning. A switch statement, a discard or a base-type arm silently turns the check off.
  - It depends on a C# 15 preview feature.
  - The second call still needs option B or D.

### B. Generic visitor

`internal abstract R Accept<R>(IVisitor<TIn, TOut, R> v)` on every node, and one generic method per node on the visitor. parsley and Kennedy and Russo use this shape.

- Pros: engine code lives outside the nodes. A visitor missing a method fails with CS0535.
- Cons:
  - A visitor whose result depends on `TOut` needs casts for nodes that fix `TOut` (CS0029, measured).
  - Generic virtual methods grow Native AOT code size quadratically with value types (dotnet/runtime#109978).
  - C# has no higher-kinded types, so each result type constructor needs its own visitor interface.

### C. `NodeKind` enum only

Engines switch on an internal enum.

- Pros: free at run time. A switch expression missing a member gives CS8509.
- Cons: it gives structure, not types. Every typed engine still needs B or D.

### D. One abstract method per engine operation

Each operation that needs a node's type arguments is an `internal abstract` member of `Parser<TIn, TOut>`, and every node overrides it. Pidgin, Parlot and Kennedy and Russo's virtual `Eval` use this shape.

- Pros:
  - A node missing an operation fails with CS0534.
  - Each override sees the specific type arguments, so it needs no casts (measured for `Tok`, `Map` and `Many`).
  - Ordinary virtual calls; no generic virtual methods.
  - No dependency on C# 15.
- Cons: the expression problem. Adding a typed engine adds a member to every node.

### Do nothing

Keep the whitepaper as written. Not possible: the compiler rejects it.

## Decision

Option D, with C's enum for consumers that need only structure. Accepted by the maintainer on 2026-10-18.

1. **Base type.** `Parser<TIn, TOut>` is a public abstract class whose constructors are all `internal`. It is not `closed`. Concrete node classes are `internal sealed`. Other assemblies cannot derive from the base (CS1729) and cannot see the nodes. User code enters the tree only through `Custom` (§ Custom primitives).
2. **Typed operations.** Each engine operation that needs a node's type arguments is an `internal abstract` member of `Parser<TIn, TOut>`, overridden by every node. The interpreter's lowering to a plan (INT-02) is the first. The general-parsing spike would add another.
   - An override forwards the node's typed fields to an ordinary generic method of the engine, for example `Lower(ctx) => ctx.Map(source, selector)`. Engine logic stays in engine classes, not node classes.
   - A node that does not implement an operation does not compile.
3. **Structural view.** Every node exposes `internal abstract NodeKind Kind` and its children in an untyped form. CORE-01 chooses the exact shape, which is internal. Consumers that need only structure use this view: the RSLN-02 analysis adapter, EBNF export, Explain and fuzzing.
   - Their dispatch over `NodeKind` is a switch expression with no discard arm, so a missing kind gives CS8509. Warnings are errors in this repository.
   - CS8524, which fires for unnamed enum values, is turned off in `.editorconfig`.
4. **No generic visitors for dispatch.** Do not add an `Accept<R>` visitor. Generic virtual methods are allowed only where a construction-time rewrite needs one, such as Pidgin-style `Select` fusion on map nodes.
5. **The source generator is outside this check.** It reads syntax and never sees node types. Its agreement with the interpreter rests on the CRL0301 fallback and the differential tests in § Equivalence (GEN-08).

Deciding reason: D is the only option that makes a missing engine operation a compile error with no casts and no generic-virtual-method code-size risk. It also does not depend on a preview language feature.

What would change this:

- A measured Native AOT size or speed problem with per-node virtual methods.
- A future C# feature that binds hidden type parameters in patterns. csharplang #3050 was closed, so this is not planned.

## Consequences

- **Easier:**
  - Adding a node: the compiler lists every operation it must implement.
  - Nodes that fix `TOut` need no special handling.
  - No preview-feature risk in the core model.
- **Harder:** adding a typed engine touches every node class. Only the interpreter needs one before 1.0, and the general-parsing spike would add the second.
- **Ruled out:** `closed` for grammar nodes, and switching on node types inside engines.
- **G7, more precisely:** compile-time completeness covers operations inside the `Creole` assembly. The generator is covered by tests, not types.
- **Risks and how they are watched:**
  - A lowering rewrite that lowers a type built from `TA`, such as `List<TA>`, through the same generic path could form a generic cycle under Native AOT (IL3054). The AOT smoke publish in CI fails on such warnings.
  - Native AOT size is not yet measured. BENCH and REL work will watch it.

## Changes made

- **Whitepaper:**
  - § Types: sample and the paragraph after the family table, linking this record.
  - § Where unions are not used: the "Grammar nodes" bullet.
  - § Custom primitives: the last sentence.
  - § Interpreted engine: the lowering sentence.
- **Cards:**
  - [CORE-01](../../active/02-core-combinators/CORE-01-grammar-node-model.md): scope and criterion 2 rewritten; moved to Building.
  - [FND-07](../../active/01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md): the `closed` bullet is now limited to the `ParseResult<T>` fallback.
  - [INT-02](../../complete/03-interpreter/INT-02-plan-lowering-and-cache.md): lowering is a per-node abstract method.
  - [RSLN-02](../../active/10-roslyn-analyzers/RSLN-02-share-the-analysis-engine-with-the-roslyn-component.md): the runtime adapter reads `NodeKind` and children.
