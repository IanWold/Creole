# ADR-0018: The generator keeps reading syntax only, for reasons that hold against Parlot 2.0

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-24 |
| Cards | [FND-10](../../active/01-foundations/FND-10-move-the-parlot-baselines-to-200.md) |
| Whitepaper | [§ Considered and deferred](../../../whitepaper.md#considered-and-deferred), [§ Questions raised in review](../../../whitepaper.md#questions-raised-in-review) |
| Research | [Parlot 2.0 baselines and generator](../research/2026-10-24-parlot-2-0-baselines-and-generator.md) |
| Supersedes | — |

## Context

[§ Considered and deferred](../../../whitepaper.md#considered-and-deferred) contrasts Parlot's generator, which "executes `[GenerateParser]` methods at build time", with Creole's, which "reads syntax only, by design". It gives two reasons: "executing user code inside the compiler is fragile in the IDE and works against incremental caching". [§ Questions raised in review](../../../whitepaper.md#questions-raised-in-review) records that a build-time execution tier was considered and decided against. [ADR-0001](0001-grammar-node-encoding-and-engine-dispatch.md) (point 5) and [ADR-0010](0010-cuts-under-dispatch-and-across-layers.md) assume a generator that reads syntax.

Parlot 2.0.0 is the first stable release of Parlot's generator. The [research note](../research/2026-10-24-parlot-2-0-baselines-and-generator.md) re-checked the contrast against it:

- **It still executes user code.** It emits the project's compilation together with build-only `.parlot.cs` grammar files, loads the result and calls the factory (finding 13). The contrast holds.
- **It does not execute in the IDE.** "IDE/design-time analysis does not execute grammar factories". At design time the generator emits stubs that throw (finding 18). So the IDE half of the first reason no longer separates Creole from Parlot.
- **It depends on the whole compilation.** Its output step combines with `CompilationProvider` (finding 19). Inference: it runs again after any edit.
- **It has no fallback.** "There is no runtime fallback for unsupported standalone code" (finding 17).
- **It runs with the build's permissions.** Parlot's docs say factories "run inside the compiler host with the build process's permissions" (finding 13).

FND-10's fourth criterion requires a decision record for any change to this argument. The paragraph's `[IncludeFiles]` clause is a separate, factual fix that FND-10 makes without one.

## Options

### A. Keep the reasons as written

Change only the `[IncludeFiles]` fact.

- Pros: no change to the argument.
- Cons: the paragraph implies that Parlot's generator is fragile in the IDE, and Parlot avoids that by not running there. A reader who knows Parlot 2.0 will discount the rest of the argument.

### B. Keep reading syntax only, and restate the reasons (recommended)

The design does not change. The paragraph credits Parlot's route for what it gains, grammars assembled by loops and helpers, and gives four reasons that hold against Parlot 2.0:

1. **The IDE and the build agree.** A generator that reads syntax produces the same code in the editor as on the command line, and it reports CRL0301 for each fallback while the grammar is being written. Parlot's IDE sees stubs that throw until a real build runs (finding 18).
2. **Caching.** Reading syntax depends only on the grammar's own code. Executing grammar code means compiling and loading the project, so it runs again after any edit (finding 19, inference).
3. **No user code runs inside the compiler.** Parlot's docs treat factories as build code that needs the same review as build scripts (finding 13).
4. **There is a fallback.** Whatever the generator cannot read runs on the interpreter. Parlot fails the build instead (finding 17).

- Pros: the argument rests on differences a reader can check.
- Cons: reason 1 holds only if the GEN cards make CRL0301 and the generated code available at design time. That is already the design, but nothing has built it yet.
- Effect on goals: none. G7 and G10 are as before.

### C. Add a build-time execution tier for subtrees the generator cannot read

Execute grammar construction at build time, as Parlot does, but only for subtrees the syntax reader misses. At design time, or when execution fails, those subtrees run on the interpreter.

- Pros: grammars built in loops, by helpers or from other assemblies would get generated code. Parlot 2.0 shows the approach works and ships it as stable.
- Cons:
  - The generator would emit, load and run the project's compilation, with reasons 1 to 3's costs for the subtrees that use the tier.
  - The generator would have two paths. Both would need the differential tests behind G7.
  - Creole already falls back correctly to the interpreter, so the tier buys speed only on fallback subtrees. Nobody has measured how often real grammars fall back; [§ Risks](../../../whitepaper.md#risks) asks for that measurement.
- Effect on goals: G7 gets harder. G8 is unaffected, since execution happens at build time only.

## Decision

**B.** The generator keeps reading syntax only. The Parlot paragraph in § Considered and deferred is replaced with:

> Parlot takes a different route to staging. Its generator compiles the project together with build-only grammar files, loads the result and runs `[GenerateParser]` methods to build the grammar graph, then emits code for it. That handles grammars assembled by loops and helpers. Creole reads syntax only, by design. The generator then produces the same code in the IDE as in a build, and CRL0301 marks each fallback as the grammar is written; Parlot's IDE sees stubs that throw until a real build. Reading syntax needs only the grammar's own code, while executing it needs the whole project compiled and loaded after every edit. No user code runs inside the compiler. And anything the generator cannot read still runs on the interpreter, where Parlot fails the build.

The build-time execution item in § Questions raised in review keeps its answer and adds that Parlot's stable generator did not change it, with a link to this record.

Deciding reason: Creole already has a correct fallback, so build-time execution would buy only speed on the subtrees that fall back. It would cost a second generator path that the engine-agreement gate must also cover. The four reasons above are the ones that still hold now that Parlot's generator is stable.

What would change this: if the fallback rate measured on real grammars ([§ Risks](../../../whitepaper.md#risks), "Generator authoring rules feel restrictive") shows that common idioms fall back with a large speed gap, option C should be reconsidered in a new record.

## Consequences

- No code or card changes. The GEN cards already plan CRL0301 and interpreter fallback.
- The whitepaper's case now depends on design-time behaviour (reason 1). If a GEN card finds that generated code or CRL0301 cannot be offered at design time, this record needs revisiting.
- Option C remains possible later. Nothing in the plan representation rules it out.

## Changes made

- **Whitepaper.**
  - [§ Considered and deferred](../../../whitepaper.md#considered-and-deferred): the Parlot paragraph is replaced with the text under Decision and links this record. The replacement also drops the stale `[IncludeFiles]` clause.
  - [§ Questions raised in review](../../../whitepaper.md#questions-raised-in-review): the build-time execution item adds that Parlot's stable generator did not change the answer, and links this record.
  - **Checked, unchanged.** § Source-generated engine's Discovery paragraph ("Parlot instead has users declare a `partial` parse method") still holds on 2.0.0 (research note, finding 16).
- **Cards.** None changed. [FND-10](../../active/01-foundations/FND-10-move-the-parlot-baselines-to-200.md) moves to Build for its factual edits.
